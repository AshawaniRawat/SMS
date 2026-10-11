using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.RolePermissions;

public interface IRolePermissionCompatibilityService
{
    bool CanAssign(RoleType roleType, PermissionScope permissionScope);
}

public sealed class RolePermissionCompatibilityService : IRolePermissionCompatibilityService
{
    public bool CanAssign(RoleType roleType, PermissionScope permissionScope) => roleType switch
    {
        RoleType.Platform => true,
        RoleType.Tenant => permissionScope is PermissionScope.Tenant or PermissionScope.School or PermissionScope.Campus or PermissionScope.Self,
        RoleType.School => permissionScope is PermissionScope.School or PermissionScope.Campus or PermissionScope.Self,
        RoleType.Campus => permissionScope is PermissionScope.Campus or PermissionScope.Self,
        _ => false
    };
}
