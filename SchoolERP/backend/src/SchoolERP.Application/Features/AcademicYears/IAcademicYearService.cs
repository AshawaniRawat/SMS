using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Enums;
namespace SchoolERP.Application.Features.AcademicYears;
public interface IAcademicYearService
{
    Task<AcademicYearDto> CreateAsync(CreateAcademicYearRequest request, CancellationToken ct = default);
    Task<AcademicYearDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AcademicYearDto?> GetCurrentAsync(Guid schoolId, CancellationToken ct = default);
    Task<AcademicYearDto?> GetByCodeAsync(Guid schoolId, string code, CancellationToken ct = default);
    Task<PagedResult<AcademicYearDto>> ListAsync(PagedRequest page, Guid? schoolId, string? search, AcademicYearStatus? status, bool? isCurrent, CancellationToken ct = default);
    Task<AcademicYearDto?> UpdateAsync(Guid id, UpdateAcademicYearRequest request, CancellationToken ct = default);
    Task<AcademicYearDto?> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<AcademicYearDto?> SetCurrentAsync(Guid id, CancellationToken ct = default);
    Task<AcademicYearDto?> CloseAsync(Guid id, CancellationToken ct = default);
    Task<AcademicYearDto?> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<AcademicYearDto?> OpenAdmissionsAsync(Guid id, OpenAdmissionsRequest request, CancellationToken ct = default);
    Task<AcademicYearDto?> CloseAdmissionsAsync(Guid id, CancellationToken ct = default);
}
