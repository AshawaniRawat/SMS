using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Infrastructure.Identity;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class RoleFoundationTests
{
    [Fact]
    public void Tenant_role_normalizes_code_generates_unique_identity_name_and_starts_active()
    {
        var tenantId = Guid.NewGuid();
        var role = ApplicationRole.CreateTenant(tenantId, "custom_coordinator", "  Custom Coordinator  ",
            RoleType.Tenant, "  Coordinates work  ", 20);
        Assert.Equal("CUSTOM_COORDINATOR", role.Code);
        Assert.Equal($"TENANT_{tenantId:N}_CUSTOM_COORDINATOR", role.Name);
        Assert.Equal("Custom Coordinator", role.DisplayName);
        Assert.Equal("Coordinates work", role.Description);
        Assert.False(role.IsSystemRole); Assert.True(role.IsActive); Assert.Equal(RoleStatus.Active, role.Status);
    }

    [Fact]
    public void Tenant_role_rejects_platform_scope_and_invalid_code()
    {
        Assert.Throws<DomainException>(() => ApplicationRole.CreateTenant(Guid.NewGuid(), "VALID", "Valid", RoleType.Platform));
        Assert.Throws<DomainException>(() => ApplicationRole.CreateTenant(Guid.NewGuid(), "HAS SPACE", "Valid", RoleType.Tenant));
        Assert.Throws<DomainException>(() => ApplicationRole.CreateTenant(Guid.Empty, "VALID", "Valid", RoleType.Tenant));
    }

    [Fact]
    public void Custom_role_lifecycle_is_safe_and_archived_is_terminal()
    {
        var role = ApplicationRole.CreateTenant(Guid.NewGuid(), "HELPER", "Helper", RoleType.Tenant);
        role.Deactivate(); Assert.False(role.IsActive); Assert.Equal(RoleStatus.Inactive, role.Status);
        role.Activate(); Assert.True(role.IsActive); Assert.Equal(RoleStatus.Active, role.Status);
        role.Archive(); Assert.False(role.IsActive); Assert.Equal(RoleStatus.Archived, role.Status);
        Assert.Throws<DomainException>(role.Activate);
    }

    [Fact]
    public void System_roles_are_immutable()
    {
        var definition = CoreRoleCatalog.All[0];
        var role = ApplicationRole.CreateSystem(definition.Id, definition.Code, definition.DisplayName,
            definition.Type, null, definition.SortOrder);
        Assert.Throws<DomainException>(() => role.Update("Changed", null, null));
        Assert.Throws<DomainException>(role.Deactivate);
        Assert.Throws<DomainException>(role.Archive);
    }

    [Fact]
    public void Core_role_ids_and_codes_are_unique()
    {
        Assert.Equal(27, CoreRoleCatalog.All.Count);
        Assert.Equal(CoreRoleCatalog.All.Count, CoreRoleCatalog.All.Select(x => x.Id).Distinct().Count());
        Assert.Equal(CoreRoleCatalog.All.Count, CoreRoleCatalog.All.Select(x => x.Code).Distinct().Count());
    }
}
