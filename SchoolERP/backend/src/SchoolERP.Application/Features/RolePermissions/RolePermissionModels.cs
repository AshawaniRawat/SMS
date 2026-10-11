using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.RolePermissions;

public sealed record RolePermissionDto(Guid PermissionId, string Code, string Name, string Module,
    string Action, PermissionScope Scope, string? Description);
public sealed record RoleWithPermissionsDto(Guid RoleId, Guid? TenantId, string Code, string DisplayName,
    RoleType Type, RoleStatus Status, bool IsSystemRole, IReadOnlyCollection<RolePermissionDto> Permissions)
{ public int PermissionCount => Permissions.Count; }
public sealed record SetRolePermissionsRequest(IReadOnlyCollection<Guid> PermissionIds);

public interface IRolePermissionService
{
    Task<Result<bool>> AssignPermissionAsync(Guid tenantId, Guid roleId, Guid permissionId, CancellationToken ct = default);
    Task<Result<bool>> RemovePermissionAsync(Guid tenantId, Guid roleId, Guid permissionId, CancellationToken ct = default);
    Task<Result<RoleWithPermissionsDto>> SetPermissionsAsync(Guid tenantId, Guid roleId,
        IReadOnlyCollection<Guid> permissionIds, CancellationToken ct = default);
    Task<RoleWithPermissionsDto?> GetRolePermissionsAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetRolePermissionCodesAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetUserEffectivePermissionCodesAsync(Guid? tenantId, Guid userId, CancellationToken ct = default);
}

public static class RolePermissionErrorCodes
{
    public const string RoleNotFound = "RolePermission.RoleNotFound";
    public const string PermissionNotFound = "RolePermission.PermissionNotFound";
    public const string RoleArchived = "RolePermission.RoleArchived";
    public const string RoleSystemManaged = "RolePermission.RoleSystemManaged";
    public const string PermissionInactive = "RolePermission.PermissionInactive";
    public const string ScopeIncompatible = "RolePermission.ScopeIncompatible";
    public const string AssignmentFailed = "RolePermission.AssignmentFailed";
    public const string Validation = "RolePermission.Validation";
}
