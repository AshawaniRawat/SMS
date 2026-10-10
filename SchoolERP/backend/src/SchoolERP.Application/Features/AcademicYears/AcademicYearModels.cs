using SchoolERP.Domain.Enums;
namespace SchoolERP.Application.Features.AcademicYears;

public sealed record AcademicYearDto(Guid Id, Guid TenantId, Guid SchoolId, string Name, string Code,
    DateOnly StartDate, DateOnly EndDate, AcademicYearStatus Status, bool IsCurrent, bool IsAdmissionOpen,
    DateOnly? AdmissionStartDate, DateOnly? AdmissionEndDate, DateOnly? LockDate, string? Description,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record CreateAcademicYearRequest(Guid SchoolId, string Name, string Code, DateOnly StartDate,
    DateOnly EndDate, string? Description = null, DateOnly? AdmissionStartDate = null,
    DateOnly? AdmissionEndDate = null, DateOnly? LockDate = null);
public sealed record UpdateAcademicYearRequest(string Name, string Code, DateOnly StartDate, DateOnly EndDate,
    string? Description = null, DateOnly? AdmissionStartDate = null, DateOnly? AdmissionEndDate = null,
    DateOnly? LockDate = null);
public sealed record OpenAdmissionsRequest(DateOnly? StartDate = null, DateOnly? EndDate = null);
