using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.SchoolDashboard;

public enum DashboardAttentionSeverity { Info = 1, Warning = 2, Critical = 3 }
public sealed record SchoolDashboardHeaderDto(Guid SchoolId, string Name, string Code, string? ShortName,
    SchoolStatus Status, string? LogoUrl, string? City, string? State, string? CountryCode);
public sealed record SchoolDashboardSummaryDto(int TotalCampuses, int ActiveCampuses, int TotalUsers,
    int ActiveUsers, int PendingUsers, int SuspendedUsers, int TotalRoles, int CustomRoles,
    int ActiveAcademicYears, bool HasCurrentAcademicYear);
public sealed record CurrentAcademicYearDto(Guid Id, string Name, string Code, DateOnly StartDate, DateOnly EndDate,
    AcademicYearStatus Status, bool IsCurrent, bool IsAdmissionOpen, DateOnly? LockDate);
public sealed record CampusPreviewDto(Guid Id, string Name, string Code, CampusStatus Status, bool IsMainCampus, string? City);
public sealed record CampusSummaryDto(int Total, int Draft, int Active, int Suspended, int Archived,
    Guid? MainCampusId, string? MainCampusName, IReadOnlyCollection<CampusPreviewDto> Preview);
public sealed record SchoolUserSummaryDto(int TotalUsers, int ActiveUsers, int PendingUsers, int SuspendedUsers,
    int LockedUsers, int DeactivatedUsers, int UnconfirmedEmailUsers);
public sealed record RoleSummaryDto(int TotalApplicableRoles, int SystemRoles, int CustomRoles, int ActiveRoles, int InactiveRoles);
public sealed record SetupHealthDto(bool SchoolProfileConfigured, bool HasCampus, bool HasMainCampus,
    bool HasCurrentAcademicYear, bool HasActiveAdminUser, bool HasRolesConfigured, int CompletionPercentage);
public sealed record DashboardAttentionItemDto(string Code, string Title, string Description,
    DashboardAttentionSeverity Severity, string? ActionPath);
public sealed record SchoolDashboardDto(SchoolDashboardHeaderDto School, SchoolDashboardSummaryDto Summary,
    CurrentAcademicYearDto? CurrentAcademicYear, CampusSummaryDto Campuses, SchoolUserSummaryDto Users,
    RoleSummaryDto Roles, SetupHealthDto SetupHealth, IReadOnlyCollection<DashboardAttentionItemDto> AttentionItems);
public interface ISchoolDashboardService { Task<SchoolDashboardDto> GetDashboardAsync(CancellationToken ct = default); }
