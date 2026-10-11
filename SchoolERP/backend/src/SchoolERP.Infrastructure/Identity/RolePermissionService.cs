using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SchoolERP.Application.Features.RolePermissions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;

namespace SchoolERP.Infrastructure.Identity;

public sealed class RolePermissionService(SchoolErpDbContext context,
    IRolePermissionCompatibilityService compatibility, ILogger<RolePermissionService> logger) : IRolePermissionService
{
    public async Task<Result<bool>> AssignPermissionAsync(Guid tenantId, Guid roleId, Guid permissionId,
        CancellationToken ct = default)
    {
        var validation = await ValidateMutation(tenantId, roleId, [permissionId], ct);
        if (!validation.IsSuccess) return Result<bool>.Fail(validation.Error!, validation.ErrorCode);
        if (await context.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, ct))
            return Result<bool>.Ok(true);
        context.RolePermissions.Add(RolePermission.Create(roleId, permissionId));
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Result<bool>.Ok(true); }
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RemovePermissionAsync(Guid tenantId, Guid roleId, Guid permissionId,
        CancellationToken ct = default)
    {
        var role = await MutableRole(tenantId, roleId, ct);
        var failure = MutationRoleFailure(role);
        if (failure is not null) return failure;
        var mapping = await context.RolePermissions.SingleOrDefaultAsync(
            x => x.RoleId == roleId && x.PermissionId == permissionId, ct);
        if (mapping is null) return Result<bool>.Ok(true);
        context.RolePermissions.Remove(mapping); await context.SaveChangesAsync(ct);
        logger.LogInformation("Role permission removed. RoleId {RoleId}, PermissionId {PermissionId}", roleId, permissionId);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<RoleWithPermissionsDto>> SetPermissionsAsync(Guid tenantId, Guid roleId,
        IReadOnlyCollection<Guid> permissionIds, CancellationToken ct = default)
    {
        if (permissionIds is null || permissionIds.Count > 500)
            return Result<RoleWithPermissionsDto>.Fail("PermissionIds is required and cannot exceed 500 entries.", RolePermissionErrorCodes.Validation);
        var desired = permissionIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (desired.Length != permissionIds.Distinct().Count())
            return Result<RoleWithPermissionsDto>.Fail("Permission IDs cannot be empty.", RolePermissionErrorCodes.Validation);
        var validation = await ValidateMutation(tenantId, roleId, desired, ct);
        if (!validation.IsSuccess)
            return Result<RoleWithPermissionsDto>.Fail(validation.Error!, validation.ErrorCode);
        var existing = await context.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct);
        var existingIds = existing.Select(x => x.PermissionId).ToHashSet();
        var desiredIds = desired.ToHashSet();
        var removed = existing.Where(x => !desiredIds.Contains(x.PermissionId)).ToArray();
        var added = desired.Where(x => !existingIds.Contains(x)).ToArray();
        context.RolePermissions.RemoveRange(removed);
        context.RolePermissions.AddRange(added.Select(x => RolePermission.Create(roleId, x)));
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Role permissions replaced. RoleId {RoleId}, Added {AddedCount}, Removed {RemovedCount}",
            roleId, added.Length, removed.Length);
        return Result<RoleWithPermissionsDto>.Ok((await GetRolePermissionsAsync(tenantId, roleId, ct))!);
    }

    public async Task<RoleWithPermissionsDto?> GetRolePermissionsAsync(Guid tenantId, Guid roleId,
        CancellationToken ct = default)
    {
        var role = await VisibleRole(tenantId, roleId, ct);
        if (role is null) return null;
        var permissions = await (from mapping in context.RolePermissions.AsNoTracking()
            join permission in context.Permissions.AsNoTracking() on mapping.PermissionId equals permission.Id
            where mapping.RoleId == roleId
            orderby permission.Module, permission.SortOrder, permission.Code
            select new RolePermissionDto(permission.Id, permission.Code, permission.Name, permission.Module,
                permission.Action, permission.Scope, permission.Description)).ToListAsync(ct);
        return new(role.Id, role.TenantId, role.Code, role.DisplayName, role.Type, role.Status,
            role.IsSystemRole, permissions);
    }

    public async Task<IReadOnlyCollection<string>> GetRolePermissionCodesAsync(Guid tenantId, Guid roleId,
        CancellationToken ct = default)
    {
        if (await VisibleRole(tenantId, roleId, ct) is null) return [];
        return await (from mapping in context.RolePermissions.AsNoTracking()
            join permission in context.Permissions.AsNoTracking() on mapping.PermissionId equals permission.Id
            where mapping.RoleId == roleId && permission.IsActive
            orderby permission.Code select permission.Code).ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<string>> GetUserEffectivePermissionCodesAsync(Guid? tenantId, Guid userId,
        CancellationToken ct = default)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId, ct);
        if (user is null) return [];
        return await (from userRole in context.Set<IdentityUserRole<Guid>>().AsNoTracking()
            join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join mapping in context.RolePermissions.AsNoTracking() on role.Id equals mapping.RoleId
            join permission in context.Permissions.AsNoTracking() on mapping.PermissionId equals permission.Id
            where userRole.UserId == userId && role.IsActive && role.Status == RoleStatus.Active && permission.IsActive
                && (tenantId.HasValue
                    ? role.TenantId == tenantId || role.IsSystemRole && role.Type != RoleType.Platform
                    : role.TenantId == null && role.Type == RoleType.Platform)
            orderby permission.Code select permission.Code).Distinct().ToListAsync(ct);
    }

    private async Task<Result<bool>> ValidateMutation(Guid tenantId, Guid roleId, IReadOnlyCollection<Guid> permissionIds,
        CancellationToken ct)
    {
        var role = await MutableRole(tenantId, roleId, ct);
        var roleFailure = MutationRoleFailure(role); if (roleFailure is not null) return roleFailure;
        if (permissionIds.Count == 0) return Result<bool>.Ok(true);
        var permissions = await context.Permissions.Where(x => permissionIds.Contains(x.Id)).ToListAsync(ct);
        if (permissions.Count != permissionIds.Count)
            return Result<bool>.Fail("One or more permissions were not found.", RolePermissionErrorCodes.PermissionNotFound);
        if (permissions.Any(x => !x.IsActive))
            return Result<bool>.Fail("Inactive permissions cannot be assigned.", RolePermissionErrorCodes.PermissionInactive);
        if (permissions.Any(x => !compatibility.CanAssign(role!.Type, x.Scope)))
            return Result<bool>.Fail("One or more permissions are incompatible with the role scope.", RolePermissionErrorCodes.ScopeIncompatible);
        return Result<bool>.Ok(true);
    }

    private async Task<ApplicationRole?> MutableRole(Guid tenantId, Guid roleId, CancellationToken ct) =>
        await context.Roles.SingleOrDefaultAsync(x => x.Id == roleId && x.TenantId == tenantId, ct) ??
        await context.Roles.SingleOrDefaultAsync(x => x.Id == roleId && x.IsSystemRole && x.Type != RoleType.Platform, ct);
    private Task<ApplicationRole?> VisibleRole(Guid tenantId, Guid roleId, CancellationToken ct) =>
        context.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == roleId &&
            (x.TenantId == tenantId || x.IsSystemRole && x.Type != RoleType.Platform), ct);
    private static Result<bool>? MutationRoleFailure(ApplicationRole? role) => role switch
    {
        null => Result<bool>.Fail("Role was not found.", RolePermissionErrorCodes.RoleNotFound),
        { IsSystemRole: true } => Result<bool>.Fail("System role permissions are platform-managed.", RolePermissionErrorCodes.RoleSystemManaged),
        { Status: RoleStatus.Archived } => Result<bool>.Fail("Archived role permissions cannot be changed.", RolePermissionErrorCodes.RoleArchived),
        _ => null
    };
}
