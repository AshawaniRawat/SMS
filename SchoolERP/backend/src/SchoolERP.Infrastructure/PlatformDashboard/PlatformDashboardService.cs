using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Features.PlatformDashboard;
using SchoolERP.Application.Security;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Security;
using SchoolERP.Infrastructure.Persistence.Context;

namespace SchoolERP.Infrastructure.PlatformDashboard;

public sealed class PlatformDashboardService(SchoolErpDbContext context, ICurrentUser currentUser,
    IUserPermissionService permissions, IPlatformExecutionScope platformScope,
    IOptions<PlatformDashboardOptions> options, HealthCheckService healthChecks, TimeProvider timeProvider)
    : IPlatformDashboardService
{
    public async Task<PlatformDashboardDto> GetDashboardAsync(PlatformDashboardQuery query, CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId || currentUser.TenantId.HasValue ||
            !await permissions.HasPermissionAsync(userId, PermissionCatalog.Platform.ViewDashboard, ct))
            throw new UnauthorizedAccessException("Platform dashboard access is denied.");
        if (query.TrendMonths is < 3 or > 24 || query.RecentTenantLimit is < 5 or > 25)
            throw new ArgumentOutOfRangeException(nameof(query));

        using var scope = platformScope.Begin();
        var tenantCounts = await context.Tenants.AsNoTracking().GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        int Tenants(TenantStatus status) => tenantCounts.GetValueOrDefault(status);
        var totalSchools = await context.Schools.AsNoTracking().CountAsync(ct);
        var activeSchools = await context.Schools.AsNoTracking().CountAsync(x => x.Status == SchoolStatus.Active, ct);
        var totalCampuses = await context.Campuses.AsNoTracking().CountAsync(ct);
        var activeCampuses = await context.Campuses.AsNoTracking().CountAsync(x => x.Status == CampusStatus.Active, ct);
        var totalYears = await context.AcademicYears.AsNoTracking().CountAsync(ct);
        var userCounts = await context.Users.AsNoTracking().GroupBy(x => x.Status)
            .Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        int Users(UserAccountStatus status) => userCounts.GetValueOrDefault(status);
        var totalUsers = userCounts.Values.Sum();
        var platformUsers = await context.Users.AsNoTracking().CountAsync(x => x.TenantId == null, ct);
        var unconfirmed = await context.Users.AsNoTracking().CountAsync(x => !x.EmailConfirmed, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var activeSessions = await context.RefreshTokens.AsNoTracking()
            .Where(x => !x.IsUsed && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .Select(x => x.SessionId).Distinct().CountAsync(ct);

        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1 - query.TrendMonths);
        var trendRows = await context.Tenants.AsNoTracking().Where(x => x.CreatedAt >= start)
            .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
            .Select(x => new { x.Key.Year, x.Key.Month, Count = x.Count() }).ToListAsync(ct);
        var trendMap = trendRows.ToDictionary(x => $"{x.Year:D4}-{x.Month:D2}", x => x.Count);
        var trend = Enumerable.Range(0, query.TrendMonths).Select(i => start.AddMonths(i))
            .Select(x => new TenantTrendPointDto(x.ToString("yyyy-MM"), trendMap.GetValueOrDefault(x.ToString("yyyy-MM")))).ToArray();

        var recent = await context.Tenants.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(query.RecentTenantLimit)
            .Select(x => new RecentTenantDto(x.Id, x.Name, x.Slug, x.Status, x.CreatedAt, x.ActivatedAt,
                context.Schools.Count(s => s.TenantId == x.Id), context.Campuses.Count(c => c.TenantId == x.Id)))
            .ToListAsync(ct);
        var warningDate = now.AddDays(-options.Value.PendingTenantWarningDays);
        var attention = await context.Tenants.AsNoTracking()
            .Where(x => x.Status == TenantStatus.Suspended || x.Status == TenantStatus.Pending && x.CreatedAt <= warningDate ||
                x.Status == TenantStatus.Active && !context.Schools.Any(s => s.TenantId == x.Id))
            .OrderBy(x => x.Status).ThenBy(x => x.CreatedAt).Take(25)
            .Select(x => new TenantAttentionDto(x.Id, x.Name, x.Status,
                x.Status == TenantStatus.Suspended ? "Suspended" : x.Status == TenantStatus.Pending ? "Pending onboarding" : "No school configured",
                x.Status == TenantStatus.Suspended ? x.SuspendedAt : x.CreatedAt)).ToListAsync(ct);

        var health = await healthChecks.CheckHealthAsync(r => r.Name == "postgresql", ct);
        var databaseHealthy = health.Status == HealthStatus.Healthy;
        var status = new TenantStatusSummaryDto(Tenants(TenantStatus.Pending), Tenants(TenantStatus.Active), Tenants(TenantStatus.Suspended), Tenants(TenantStatus.Archived));
        var summary = new PlatformSummaryDto(tenantCounts.Values.Sum(), status.Active, status.Pending, status.Suspended,
            status.Archived, totalSchools, activeSchools, totalCampuses, activeCampuses, totalUsers,
            Users(UserAccountStatus.Active), totalYears);
        var users = new PlatformUserSummaryDto(totalUsers, Users(UserAccountStatus.Active), Users(UserAccountStatus.Pending),
            Users(UserAccountStatus.Suspended), Users(UserAccountStatus.Locked), Users(UserAccountStatus.Deactivated),
            platformUsers, totalUsers - platformUsers, unconfirmed, activeSessions);
        return new(summary, status, users, trend, recent, attention,
            new(true, databaseHealthy, databaseHealthy ? "Healthy" : "Degraded", now));
    }
}
