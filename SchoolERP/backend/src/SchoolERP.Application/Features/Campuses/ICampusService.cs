using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Enums;
namespace SchoolERP.Application.Features.Campuses;
public interface ICampusService
{
    Task<CampusDto> CreateAsync(CreateCampusRequest request, CancellationToken ct = default);
    Task<CampusDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CampusDto?> GetForSchoolAsync(Guid schoolId, Guid campusId, CancellationToken ct = default);
    Task<CampusDto?> GetByCodeAsync(Guid schoolId, string code, CancellationToken ct = default);
    Task<PagedResult<CampusDto>> ListAsync(PagedRequest page, Guid? schoolId, string? search, CampusStatus? status, bool? isMain, CancellationToken ct = default);
    Task<PagedResult<CampusDto>?> ListForSchoolAsync(PagedRequest page, Guid schoolId, string? search, CampusStatus? status, bool? isMain, CancellationToken ct = default);
    Task<CampusDto?> UpdateAsync(Guid id, CampusProfileRequest request, CancellationToken ct = default);
    Task<CampusDto?> ChangeCodeAsync(Guid id, ChangeCampusCodeRequest request, CancellationToken ct = default);
    Task<CampusDto?> SetMainAsync(Guid id, CancellationToken ct = default);
    Task<CampusDto?> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<CampusDto?> SuspendAsync(Guid id, CancellationToken ct = default);
    Task<CampusDto?> ReactivateAsync(Guid id, CancellationToken ct = default);
    Task<CampusDto?> ArchiveAsync(Guid id, CancellationToken ct = default);
}
