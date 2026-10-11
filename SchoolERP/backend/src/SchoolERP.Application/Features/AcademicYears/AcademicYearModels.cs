using SchoolERP.Domain.Enums;
namespace SchoolERP.Application.Features.AcademicYears;

public sealed record AcademicYearDto(Guid Id, Guid TenantId, Guid SchoolId, string Name, string Code,
    DateOnly StartDate, DateOnly EndDate, AcademicYearStatus Status, bool IsCurrent, bool IsAdmissionOpen,
    DateOnly? AdmissionStartDate, DateOnly? AdmissionEndDate, DateOnly? LockDate, string? Description,
    DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record AcademicYearListItemDto(Guid Id,string Name,string Code,DateOnly StartDate,DateOnly EndDate,AcademicYearStatus Status,bool IsCurrent,bool IsAdmissionOpen,DateOnly? AdmissionStartDate,DateOnly? AdmissionEndDate,DateOnly? LockDate,DateTime CreatedAt,DateTime? UpdatedAt);
public sealed record AcademicYearDetailsDto(Guid Id,string Name,string Code,DateOnly StartDate,DateOnly EndDate,AcademicYearStatus Status,bool IsCurrent,bool IsAdmissionOpen,DateOnly? AdmissionStartDate,DateOnly? AdmissionEndDate,DateOnly? LockDate,string? Description,DateTime CreatedAt,DateTime? UpdatedAt);
public sealed record GetAcademicYearsQuery(int PageNumber=1,int PageSize=20,string? Search=null,AcademicYearStatus? Status=null,bool? IsCurrent=null,bool? IsAdmissionOpen=null,string? SortBy=null,string? SortDirection=null);

public sealed record CreateAcademicYearRequest(string Name, string Code, DateOnly StartDate,
    DateOnly EndDate, string? Description = null, DateOnly? AdmissionStartDate = null,
    DateOnly? AdmissionEndDate = null, DateOnly? LockDate = null)
{ public CreateAcademicYearRequest(Guid _,string name,string code,DateOnly startDate,DateOnly endDate,string? description=null,DateOnly? admissionStartDate=null,DateOnly? admissionEndDate=null,DateOnly? lockDate=null):this(name,code,startDate,endDate,description,admissionStartDate,admissionEndDate,lockDate){} }
public sealed record UpdateAcademicYearRequest(string Name, string Code, DateOnly StartDate, DateOnly EndDate,
    string? Description = null, DateOnly? AdmissionStartDate = null, DateOnly? AdmissionEndDate = null,
    DateOnly? LockDate = null);
public sealed record OpenAdmissionsRequest(DateOnly? StartDate = null, DateOnly? EndDate = null);
public sealed record SetAcademicYearLockDateRequest(DateOnly? LockDate);
