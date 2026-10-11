using SchoolERP.Domain.Common;

namespace SchoolERP.Domain.Entities;

public sealed class RolePermission : AuditableEntity
{
    private RolePermission() { }
    private RolePermission(Guid roleId, Guid permissionId, bool isSystemAssigned)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(roleId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(permissionId, Guid.Empty);
        RoleId = roleId; PermissionId = permissionId; IsSystemAssigned = isSystemAssigned;
    }
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public bool IsSystemAssigned { get; private set; }
    public Permission Permission { get; private set; } = null!;
    public static RolePermission Create(Guid roleId, Guid permissionId, bool isSystemAssigned = false) =>
        new(roleId, permissionId, isSystemAssigned);
}
