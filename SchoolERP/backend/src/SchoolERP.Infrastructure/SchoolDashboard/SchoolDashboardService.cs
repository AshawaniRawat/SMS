using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Exceptions;
using SchoolERP.Application.Features.SchoolDashboard;
using SchoolERP.Application.Features.Schools;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;

namespace SchoolERP.Infrastructure.SchoolDashboard;

public sealed class SchoolDashboardService(SchoolErpDbContext context,
    ICurrentSchoolResolver currentSchool) : ISchoolDashboardService
{
    public async Task<SchoolDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var schoolId = await currentSchool.ResolveAsync(ct);
        var school = await context.Schools.AsNoTracking().SingleOrDefaultAsync(x => x.Id == schoolId, ct)
            ?? throw new ResourceNotFoundException("School");
        var tenantId = school.TenantId;
        var campusCounts = await context.Campuses.AsNoTracking().Where(x => x.SchoolId == schoolId)
            .GroupBy(x => x.Status).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        int Campus(CampusStatus status) => campusCounts.GetValueOrDefault(status);
        var main = await context.Campuses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsMainCampus)
            .Select(x => new { x.Id, x.Name }).SingleOrDefaultAsync(ct);
        var preview = await context.Campuses.AsNoTracking().Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.IsMainCampus).ThenBy(x => x.Name).Take(5)
            .Select(x => new CampusPreviewDto(x.Id, x.Name, x.Code, x.Status, x.IsMainCampus, x.City)).ToListAsync(ct);
        var currentYear = await context.AcademicYears.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsCurrent)
            .Select(x => new CurrentAcademicYearDto(x.Id, x.Name, x.Code, x.StartDate, x.EndDate, x.Status,
                x.IsCurrent, x.IsAdmissionOpen, x.LockDate)).SingleOrDefaultAsync(ct);
        var activeYears = await context.AcademicYears.AsNoTracking().CountAsync(x => x.SchoolId == schoolId && x.Status == AcademicYearStatus.Active, ct);
        var campusIds = context.Campuses.AsNoTracking().Where(x => x.SchoolId == schoolId).Select(x => x.Id);
        var scopedUsers = context.Users.AsNoTracking().Where(x => x.TenantId == tenantId &&
            (x.SchoolId == schoolId || x.CampusId.HasValue && campusIds.Contains(x.CampusId.Value)));
        var userCounts = await scopedUsers.GroupBy(x => x.Status).Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        int User(UserAccountStatus status) => userCounts.GetValueOrDefault(status);
        var unconfirmed = await scopedUsers.CountAsync(x => !x.EmailConfirmed, ct);
        var roles = context.Roles.AsNoTracking().Where(x => x.TenantId == tenantId ||
            x.TenantId == null && x.Type != RoleType.Platform);
        var roleCounts = await roles.GroupBy(x => new { x.IsSystemRole, x.IsActive })
            .Select(x => new { x.Key, Count = x.Count() }).ToListAsync(ct);
        var systemRoles = roleCounts.Where(x => x.Key.IsSystemRole).Sum(x => x.Count);
        var customRoles = roleCounts.Where(x => !x.Key.IsSystemRole).Sum(x => x.Count);
        var activeRoles = roleCounts.Where(x => x.Key.IsActive).Sum(x => x.Count);
        var totalRoles = systemRoles + customRoles;
        var hasAdmin = await (from assignment in context.Set<IdentityUserRole<Guid>>().AsNoTracking()
            join user in scopedUsers on assignment.UserId equals user.Id
            join role in context.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where user.IsActive && (role.Code == SystemRoleCodes.SchoolAdmin || role.Code == SystemRoleCodes.TenantOwner)
            select assignment).AnyAsync(ct);
        var profile = !string.IsNullOrWhiteSpace(school.Name) && !string.IsNullOrWhiteSpace(school.Code);
        var checks = new[] { profile, campusCounts.Values.Sum() > 0, main is not null, currentYear is not null, hasAdmin, activeRoles > 0 };
        var setup = new SetupHealthDto(checks[0], checks[1], checks[2], checks[3], checks[4], checks[5],
            (int)Math.Round(checks.Count(x => x) * 100d / checks.Length));
        var attention = BuildAttention(school.Status, setup, unconfirmed, Campus(CampusStatus.Suspended));
        var users = new SchoolUserSummaryDto(userCounts.Values.Sum(), User(UserAccountStatus.Active), User(UserAccountStatus.Pending),
            User(UserAccountStatus.Suspended), User(UserAccountStatus.Locked), User(UserAccountStatus.Deactivated), unconfirmed);
        var roleSummary = new RoleSummaryDto(totalRoles, systemRoles, customRoles, activeRoles, totalRoles - activeRoles);
        var campusSummary = new CampusSummaryDto(campusCounts.Values.Sum(), Campus(CampusStatus.Draft), Campus(CampusStatus.Active),
            Campus(CampusStatus.Suspended), Campus(CampusStatus.Archived), main?.Id, main?.Name, preview);
        var summary = new SchoolDashboardSummaryDto(campusSummary.Total, campusSummary.Active, users.TotalUsers,
            users.ActiveUsers, users.PendingUsers, users.SuspendedUsers, totalRoles, customRoles, activeYears, currentYear is not null);
        return new(new(school.Id, school.Name, school.Code, school.ShortName, school.Status, school.LogoUrl,
            school.City, school.State, school.CountryCode), summary, currentYear, campusSummary, users, roleSummary, setup, attention);
    }

    private static IReadOnlyCollection<DashboardAttentionItemDto> BuildAttention(SchoolStatus status, SetupHealthDto setup, int unconfirmed, int suspendedCampuses)
    {
        var rows = new List<DashboardAttentionItemDto>();
        if (status != SchoolStatus.Active) rows.Add(new("SchoolStatus", "School is not active", $"The school status is {status}.", DashboardAttentionSeverity.Critical, "/settings/school"));
        if (!setup.HasMainCampus) rows.Add(new("MainCampusMissing", "No main campus configured", "Choose a main campus for the school.", DashboardAttentionSeverity.Warning, "/campuses"));
        if (!setup.HasCurrentAcademicYear) rows.Add(new("CurrentAcademicYearMissing", "No current academic year", "Select the current academic year.", DashboardAttentionSeverity.Warning, "/academic-years"));
        if (!setup.HasActiveAdminUser) rows.Add(new("AdminMissing", "No active school administrator", "Assign an active School Admin or Tenant Owner.", DashboardAttentionSeverity.Warning, "/users"));
        if (unconfirmed > 0) rows.Add(new("EmailConfirmation", "Email confirmation pending", $"{unconfirmed} user account(s) are unconfirmed.", DashboardAttentionSeverity.Info, "/users"));
        if (suspendedCampuses > 0) rows.Add(new("SuspendedCampus", "Suspended campuses", $"{suspendedCampuses} campus(es) are suspended.", DashboardAttentionSeverity.Warning, "/campuses"));
        return rows.OrderByDescending(x => x.Severity).ToArray();
    }
}
