using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Permissions;

public sealed record PermissionDto(Guid Id, string Code, string Name, string Module, string Action,
    string? Description, PermissionScope Scope, bool IsActive, int SortOrder, string? Group);

public sealed record PermissionModuleDto(string Module, int Count);

public interface IPermissionCatalogService
{
    Task<PagedResult<PermissionDto>> GetPermissionsAsync(PagedRequest page, string? module = null,
        PermissionScope? scope = null, bool? isActive = null, string? search = null,
        CancellationToken cancellationToken = default);
    Task<PermissionDto?> GetPermissionByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PermissionModuleDto>> GetModulesAsync(bool? isActive = null,
        CancellationToken cancellationToken = default);
}
