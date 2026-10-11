using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolERP.Application.Features.RolePermissions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Security;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Seed;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RolePermissionTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Custom_role_assign_remove_bulk_and_effective_permissions_are_safe_and_idempotent()
    {
        await using var context = database.CreateContext(platformAccess: true);
        await new PermissionSeeder(context).SeedAsync();
        var tenant = Tenant.Create("Role Permission Tenant", $"rp-{Guid.NewGuid():N}"); tenant.Activate(DateTime.UtcNow);
        context.Tenants.Add(tenant); await context.SaveChangesAsync();
        var role = ApplicationRole.CreateTenant(tenant.Id, "ACADEMIC_HELPER", "Academic Helper", RoleType.School);
        var otherRole = ApplicationRole.CreateTenant(tenant.Id, "SECOND_HELPER", "Second Helper", RoleType.School);
        context.Roles.AddRange(role, otherRole);
        var user = ApplicationUser.Create("Effective", "User", $"effective-{Guid.NewGuid():N}@example.com", tenant.Id, activate: true);
        context.Users.Add(user); await context.SaveChangesAsync();
        context.Set<IdentityUserRole<Guid>>().AddRange(new() { UserId = user.Id, RoleId = role.Id }, new() { UserId = user.Id, RoleId = otherRole.Id });
        await context.SaveChangesAsync();
        var service = Service(context);
        var student = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Student.View);
        var attendance = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Attendance.View);
        var homework = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Homework.View);
        Assert.True((await service.AssignPermissionAsync(tenant.Id, role.Id, student.Id)).IsSuccess);
        Assert.True((await service.AssignPermissionAsync(tenant.Id, role.Id, student.Id)).IsSuccess);
        Assert.Equal(1, await context.RolePermissions.CountAsync(x => x.RoleId == role.Id && x.PermissionId == student.Id));
        var bulk = await service.SetPermissionsAsync(tenant.Id, role.Id, [student.Id, student.Id, attendance.Id]);
        Assert.True(bulk.IsSuccess); Assert.Equal(2, bulk.Value!.PermissionCount);
        await service.AssignPermissionAsync(tenant.Id, otherRole.Id, student.Id);
        await service.AssignPermissionAsync(tenant.Id, otherRole.Id, homework.Id);
        Assert.Equal([PermissionCatalog.Attendance.View, PermissionCatalog.Homework.View, PermissionCatalog.Student.View],
            await service.GetUserEffectivePermissionCodesAsync(tenant.Id, user.Id));
        otherRole.Deactivate(); await context.SaveChangesAsync();
        Assert.Equal([PermissionCatalog.Attendance.View, PermissionCatalog.Student.View],
            await service.GetUserEffectivePermissionCodesAsync(tenant.Id, user.Id));
        student.Deactivate(); await context.SaveChangesAsync();
        Assert.Equal([PermissionCatalog.Attendance.View],
            await service.GetUserEffectivePermissionCodesAsync(tenant.Id, user.Id));
        Assert.True((await service.RemovePermissionAsync(tenant.Id, role.Id, attendance.Id)).IsSuccess);
        Assert.True((await service.RemovePermissionAsync(tenant.Id, role.Id, attendance.Id)).IsSuccess);
    }

    [PostgresFact]
    public async Task Mutation_rejects_cross_tenant_system_archived_inactive_and_incompatible_without_partial_bulk_change()
    {
        await using var context = database.CreateContext(platformAccess: true);
        await new PermissionSeeder(context).SeedAsync();
        var tenant = Tenant.Create("Scope Tenant", $"scope-{Guid.NewGuid():N}"); tenant.Activate(DateTime.UtcNow);
        var other = Tenant.Create("Other Scope Tenant", $"other-scope-{Guid.NewGuid():N}"); other.Activate(DateTime.UtcNow);
        context.Tenants.AddRange(tenant, other); await context.SaveChangesAsync();
        var role = ApplicationRole.CreateTenant(tenant.Id, "CAMPUS_HELPER", "Campus Helper", RoleType.Campus);
        context.Roles.Add(role); await context.SaveChangesAsync();
        var service = Service(context);
        var campus = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Campus.View);
        var school = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.School.View);
        var platform = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Platform.ViewDashboard);
        var inactive = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Campus.Edit);
        Assert.Equal(RolePermissionErrorCodes.RoleNotFound, (await service.AssignPermissionAsync(other.Id, role.Id, campus.Id)).ErrorCode);
        Assert.Equal(RolePermissionErrorCodes.ScopeIncompatible, (await service.AssignPermissionAsync(tenant.Id, role.Id, school.Id)).ErrorCode);
        inactive.Deactivate(); await context.SaveChangesAsync();
        Assert.Equal(RolePermissionErrorCodes.PermissionInactive, (await service.AssignPermissionAsync(tenant.Id, role.Id, inactive.Id)).ErrorCode);
        Assert.True((await service.AssignPermissionAsync(tenant.Id, role.Id, campus.Id)).IsSuccess);
        var failed = await service.SetPermissionsAsync(tenant.Id, role.Id, [campus.Id, platform.Id]);
        Assert.False(failed.IsSuccess);
        Assert.Equal([campus.Id], await context.RolePermissions.Where(x => x.RoleId == role.Id).Select(x => x.PermissionId).ToListAsync());
        role.Archive(); await context.SaveChangesAsync();
        Assert.Equal(RolePermissionErrorCodes.RoleArchived, (await service.AssignPermissionAsync(tenant.Id, role.Id, campus.Id)).ErrorCode);
        var system = await context.Roles.SingleAsync(x => x.Code == SystemRoleCodes.SchoolAdmin);
        Assert.Equal(RolePermissionErrorCodes.RoleSystemManaged, (await service.AssignPermissionAsync(tenant.Id, system.Id, campus.Id)).ErrorCode);
    }

    [PostgresFact]
    public async Task System_mapping_seed_is_idempotent_complete_and_does_not_touch_custom_roles()
    {
        await using var context = database.CreateContext(platformAccess: true);
        await new PermissionSeeder(context).SeedAsync();
        var tenant = Tenant.Create("Seed Safety Tenant", $"seed-safe-{Guid.NewGuid():N}"); tenant.Activate(DateTime.UtcNow);
        context.Tenants.Add(tenant); await context.SaveChangesAsync();
        var custom = ApplicationRole.CreateTenant(tenant.Id, "CUSTOM", "Custom", RoleType.School); context.Roles.Add(custom);
        await context.SaveChangesAsync();
        var permission = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Student.View);
        context.RolePermissions.Add(RolePermission.Create(custom.Id, permission.Id)); await context.SaveChangesAsync();
        var seeder = new RolePermissionSeeder(context); await seeder.SeedAsync(); var count = await context.RolePermissions.CountAsync();
        await seeder.SeedAsync(); Assert.Equal(count, await context.RolePermissions.CountAsync());
        Assert.True(await context.RolePermissions.AnyAsync(x => x.RoleId == custom.Id && x.PermissionId == permission.Id && !x.IsSystemAssigned));
        var super = await context.Roles.SingleAsync(x => x.Code == SystemRoleCodes.PlatformSuperAdmin);
        Assert.Equal(await context.Permissions.CountAsync(x => x.IsActive), await context.RolePermissions.CountAsync(x => x.RoleId == super.Id));
        foreach (var code in new[] { SystemRoleCodes.SchoolAdmin, SystemRoleCodes.Principal, SystemRoleCodes.Teacher, SystemRoleCodes.Accountant, SystemRoleCodes.HrManager })
            Assert.True(await context.RolePermissions.AnyAsync(x => x.RoleId == context.Roles.Single(r => r.Code == code).Id));
    }

    private static RolePermissionService Service(Infrastructure.Persistence.Context.SchoolErpDbContext context) =>
        new(context, new RolePermissionCompatibilityService(), NullLogger<RolePermissionService>.Instance);
}
