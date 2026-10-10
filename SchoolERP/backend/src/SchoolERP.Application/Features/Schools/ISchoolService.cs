using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Schools;

public interface ISchoolService
{
    Task<SchoolDto> CreateAsync(CreateSchoolRequest request, CancellationToken cancellationToken = default);
    Task<SchoolDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SchoolDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<PagedResult<SchoolDto>> ListAsync(PagedRequest page, string? search, SchoolStatus? status,
        SchoolBoardType? boardType, CancellationToken cancellationToken = default);
    Task<SchoolDto?> UpdateProfileAsync(Guid id, SchoolProfileRequest request, CancellationToken cancellationToken = default);
    Task<SchoolDto?> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SchoolDto?> SuspendAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SchoolDto?> ReactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SchoolDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
