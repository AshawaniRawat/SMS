using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Tenants;

public interface ITenantService
{
    Task<TenantDto> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<PagedResult<TenantDto>> ListAsync(PagedRequest page, string? search, TenantStatus? status, CancellationToken cancellationToken = default);
    Task<TenantDto?> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDto?> SuspendAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDto?> ReactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
