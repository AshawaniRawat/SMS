using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Permissions;

public sealed class PermissionCatalogService(IApplicationDbContext context) : IPermissionCatalogService
{
    public async Task<PagedResult<PermissionDto>> GetPermissionsAsync(PagedRequest page, string? module = null,
        PermissionScope? scope = null, bool? isActive = null, string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Permissions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(module))
        {
            var value = module.Trim();
            query = query.Where(x => x.Module == value);
        }
        if (scope.HasValue) query = query.Where(x => x.Scope == scope.Value);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.Code.Contains(value) || x.Name.Contains(value) ||
                (x.Description != null && x.Description.Contains(value)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize)
            .Select(x => Map(x)).ToListAsync(cancellationToken);
        return new(items, page.PageNumber, page.PageSize, total);
    }

    public Task<PermissionDto?> GetPermissionByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var value = code.Trim();
        return context.Permissions.AsNoTracking().Where(x => x.Code == value)
            .Select(x => Map(x)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PermissionModuleDto>> GetModulesAsync(bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Permissions.AsNoTracking();
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        return await query.GroupBy(x => x.Module)
            .Select(x => new PermissionModuleDto(x.Key, x.Count()))
            .OrderBy(x => x.Module).ToListAsync(cancellationToken);
    }

    private static PermissionDto Map(Permission value) => new(value.Id, value.Code, value.Name, value.Module,
        value.Action, value.Description, value.Scope, value.IsActive, value.SortOrder, value.Group);
}
