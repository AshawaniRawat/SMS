using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.PlatformDashboard;

public sealed record PlatformDashboardQuery(int TrendMonths = 12, int RecentTenantLimit = 10);
public sealed record PlatformSummaryDto(int TotalTenants, int ActiveTenants, int PendingTenants,
    int SuspendedTenants, int ArchivedTenants, int TotalSchools, int ActiveSchools, int TotalCampuses,
    int ActiveCampuses, int TotalUsers, int ActiveUsers, int TotalAcademicYears);
public sealed record TenantStatusSummaryDto(int Pending, int Active, int Suspended, int Archived);
public sealed record PlatformUserSummaryDto(int TotalUsers, int ActiveUsers, int PendingUsers,
    int SuspendedUsers, int LockedUsers, int DeactivatedUsers, int PlatformUsers, int TenantUsers,
    int UnconfirmedEmailUsers, int ActiveRefreshSessions);
public sealed record TenantTrendPointDto(string Period, int Count);
public sealed record RecentTenantDto(Guid TenantId, string Name, string Slug, TenantStatus Status,
    DateTime CreatedAt, DateTime? ActivatedAt, int SchoolCount, int CampusCount);
public sealed record TenantAttentionDto(Guid TenantId, string Name, TenantStatus Status, string Reason, DateTime? Since);
public sealed record SystemHealthSummaryDto(bool ApiHealthy, bool DatabaseHealthy, string OverallStatus, DateTime CheckedAtUtc);
public sealed record PlatformDashboardDto(PlatformSummaryDto Summary, TenantStatusSummaryDto TenantStatus,
    PlatformUserSummaryDto Users, IReadOnlyCollection<TenantTrendPointDto> OnboardingTrend,
    IReadOnlyCollection<RecentTenantDto> RecentTenants, IReadOnlyCollection<TenantAttentionDto> TenantsRequiringAttention,
    SystemHealthSummaryDto SystemHealth);

public interface IPlatformDashboardService
{
    Task<PlatformDashboardDto> GetDashboardAsync(PlatformDashboardQuery query, CancellationToken cancellationToken = default);
}

public sealed class PlatformDashboardOptions
{
    public const string SectionName = "PlatformDashboard";
    public int PendingTenantWarningDays { get; set; } = 7;
}
