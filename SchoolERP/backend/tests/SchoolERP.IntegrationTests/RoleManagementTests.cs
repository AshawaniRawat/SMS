using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Roles;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RoleManagementTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Tenant_codes_are_scoped_internal_names_global_and_cross_tenant_assignment_is_blocked()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        var tenantA = Tenant.Create("Role Tenant A", $"role-a-{Guid.NewGuid():N}"); tenantA.Activate(DateTime.UtcNow);
        var tenantB = Tenant.Create("Role Tenant B", $"role-b-{Guid.NewGuid():N}"); tenantB.Activate(DateTime.UtcNow);
        context.AddRange(tenantA, tenantB); await context.SaveChangesAsync();
        using var roleManager = RoleManager(context); using var userManager = UserManager(context);
        var service = Service(context, roleManager, userManager);
        var roleA = await service.CreateTenantRoleAsync(tenantA.Id, new("ACCOUNT_HELPER", "Account Helper", null, RoleType.Tenant));
        var duplicate = await service.CreateTenantRoleAsync(tenantA.Id, new("account_helper", "Duplicate", null, RoleType.Tenant));
        var roleB = await service.CreateTenantRoleAsync(tenantB.Id, new("ACCOUNT_HELPER", "Account Helper", null, RoleType.Tenant));
        Assert.True(roleA.IsSuccess); Assert.Equal(RoleErrorCodes.DuplicateCode, duplicate.ErrorCode); Assert.True(roleB.IsSuccess);
        Assert.NotEqual((await context.Roles.FindAsync(roleA.Value!.Id))!.Name, (await context.Roles.FindAsync(roleB.Value!.Id))!.Name);
        var user = ApplicationUser.Create("Role", "User", $"role-user-{Guid.NewGuid():N}@example.com", tenantA.Id, activate: true);
        Assert.True((await userManager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        Assert.True((await service.AssignRoleAsync(tenantA.Id, user.Id, roleA.Value.Id)).IsSuccess);
        Assert.Equal(RoleErrorCodes.NotFound, (await service.AssignRoleAsync(tenantA.Id, user.Id, roleB.Value.Id)).ErrorCode);
    }

    [PostgresFact]
    public async Task Assignment_scope_inactive_and_archive_guards_are_enforced()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        var tenant = Tenant.Create("Role Guard Tenant", $"role-guard-{Guid.NewGuid():N}"); tenant.Activate(DateTime.UtcNow);
        context.Add(tenant); await context.SaveChangesAsync();
        using var roleManager = RoleManager(context); using var userManager = UserManager(context);
        var service = Service(context, roleManager, userManager);
        var schoolRole = (await service.CreateTenantRoleAsync(tenant.Id,
            new("SCHOOL_HELPER", "School Helper", null, RoleType.School))).Value!;
        var tenantUser = ApplicationUser.Create("Tenant", "Only", $"tenant-only-{Guid.NewGuid():N}@example.com", tenant.Id, activate: true);
        Assert.True((await userManager.CreateAsync(tenantUser, "Valid#Pass1")).Succeeded);
        Assert.Equal(RoleErrorCodes.ScopeMismatch,
            (await service.AssignRoleAsync(tenant.Id, tenantUser.Id, schoolRole.Id)).ErrorCode);
        var tenantRole = (await service.CreateTenantRoleAsync(tenant.Id,
            new("HELPER", "Helper", null, RoleType.Tenant))).Value!;
        Assert.True((await service.AssignRoleAsync(tenant.Id, tenantUser.Id, tenantRole.Id)).IsSuccess);
        Assert.Equal(RoleErrorCodes.HasAssignedUsers, (await service.ArchiveAsync(tenant.Id, tenantRole.Id)).ErrorCode);
        Assert.True((await service.RemoveRoleAsync(tenant.Id, tenantUser.Id, tenantRole.Id)).IsSuccess);
        Assert.True((await service.DeactivateAsync(tenant.Id, tenantRole.Id)).IsSuccess);
        Assert.Equal(RoleErrorCodes.Inactive,
            (await service.AssignRoleAsync(tenant.Id, tenantUser.Id, tenantRole.Id)).ErrorCode);
        Assert.True((await service.ArchiveAsync(tenant.Id, tenantRole.Id)).IsSuccess);
        Assert.False((await service.ActivateAsync(tenant.Id, tenantRole.Id)).IsSuccess);
    }

    [PostgresFact]
    public async Task Migration_seeds_expected_protected_system_roles()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        var roles = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            context.Roles.Where(x => x.IsSystemRole));
        Assert.Equal(CoreRoleCatalog.All.Count, roles.Count);
        Assert.Contains(roles, x => x.Code == "PLATFORM_SUPER_ADMIN" && x.Type == RoleType.Platform);
        Assert.All(roles, x => { Assert.Null(x.TenantId); Assert.True(x.IsActive); Assert.Equal(RoleStatus.Active, x.Status); });
    }

    private static RoleManagementService Service(SchoolErpDbContext context, RoleManager<ApplicationRole> roles,
        UserManager<ApplicationUser> users) => new(context, roles, users,
        new CreateTenantRoleRequestValidator(), new UpdateRoleRequestValidator());

    private static RoleManager<ApplicationRole> RoleManager(SchoolErpDbContext context)
    {
        var store = new RoleStore<ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new(store, [new RoleValidator<ApplicationRole>()], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), NullLogger<RoleManager<ApplicationRole>>.Instance);
    }
    private static UserManager<ApplicationUser> UserManager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions()); options.Value.User.RequireUniqueEmail = true;
        var store = new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new(store, options, new PasswordHasher<ApplicationUser>(), [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
