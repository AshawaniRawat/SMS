using SchoolERP.Domain.Enums;namespace SchoolERP.Application.Features.SchoolClasses;
public sealed record CreateSchoolClassRequest(Guid AcademicYearId,string Name,string Code,string? ShortName,int DisplayOrder,EducationLevel? EducationLevel,string? Description);
public sealed record UpdateSchoolClassRequest(string Name,string? ShortName,int DisplayOrder,EducationLevel? EducationLevel,string? Description);
public sealed record ChangeSchoolClassCodeRequest(string Code);
public sealed record SchoolClassListItemDto(Guid Id,Guid AcademicYearId,string AcademicYearName,string Name,string Code,string? ShortName,int DisplayOrder,EducationLevel? EducationLevel,SchoolClassStatus Status,DateTime CreatedAt,DateTime? UpdatedAt);
public sealed record SchoolClassDetailsDto(Guid Id,Guid AcademicYearId,string AcademicYearName,string AcademicYearCode,string Name,string Code,string? ShortName,int DisplayOrder,EducationLevel? EducationLevel,SchoolClassStatus Status,string? Description,DateTime? ActivatedAt,DateTime? DeactivatedAt,DateTime? ArchivedAt,DateTime CreatedAt,DateTime? UpdatedAt);
public sealed record GetSchoolClassesQuery(int PageNumber=1,int PageSize=20,Guid? AcademicYearId=null,SchoolClassStatus? Status=null,EducationLevel? EducationLevel=null,string? Search=null,string? SortBy=null,string? SortDirection=null);
