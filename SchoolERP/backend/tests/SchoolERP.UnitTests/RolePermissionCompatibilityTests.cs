using SchoolERP.Application.Features.RolePermissions;
using SchoolERP.Domain.Enums;
using Xunit;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Domain.Security;

namespace SchoolERP.UnitTests;

public sealed class RolePermissionCompatibilityTests
{
    private readonly RolePermissionCompatibilityService _policy = new();

    [Theory]
    [InlineData(RoleType.Platform, PermissionScope.Platform, true)]
    [InlineData(RoleType.Platform, PermissionScope.Tenant, true)]
    [InlineData(RoleType.Platform, PermissionScope.School, true)]
    [InlineData(RoleType.Platform, PermissionScope.Campus, true)]
    [InlineData(RoleType.Platform, PermissionScope.Self, true)]
    [InlineData(RoleType.Tenant, PermissionScope.Platform, false)]
    [InlineData(RoleType.Tenant, PermissionScope.Tenant, true)]
    [InlineData(RoleType.Tenant, PermissionScope.School, true)]
    [InlineData(RoleType.Tenant, PermissionScope.Campus, true)]
    [InlineData(RoleType.Tenant, PermissionScope.Self, true)]
    [InlineData(RoleType.School, PermissionScope.Platform, false)]
    [InlineData(RoleType.School, PermissionScope.Tenant, false)]
    [InlineData(RoleType.School, PermissionScope.School, true)]
    [InlineData(RoleType.School, PermissionScope.Campus, true)]
    [InlineData(RoleType.School, PermissionScope.Self, true)]
    [InlineData(RoleType.Campus, PermissionScope.Platform, false)]
    [InlineData(RoleType.Campus, PermissionScope.Tenant, false)]
    [InlineData(RoleType.Campus, PermissionScope.School, false)]
    [InlineData(RoleType.Campus, PermissionScope.Campus, true)]
    [InlineData(RoleType.Campus, PermissionScope.Self, true)]
    public void Matrix_is_conservative(RoleType role, PermissionScope permission, bool expected) =>
        Assert.Equal(expected, _policy.CanAssign(role, permission));

    [Fact]
    public void Seeded_default_sets_are_catalog_backed_and_scope_compatible()
    {
        foreach (var set in SystemRolePermissionSets.All)
        {
            var role = CoreRoleCatalog.All.Single(x => x.Code == set.Key);
            Assert.Equal(set.Value.Count, set.Value.Distinct(StringComparer.Ordinal).Count());
            foreach (var code in set.Value)
            {
                var permission = PermissionCatalog.FindByCode(code);
                Assert.NotNull(permission);
                Assert.True(_policy.CanAssign(role.Type, permission!.Scope), $"{set.Key} cannot receive {code}");
            }
        }
    }
}
