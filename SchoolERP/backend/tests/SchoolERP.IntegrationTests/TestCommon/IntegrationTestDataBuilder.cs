using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;

namespace SchoolERP.IntegrationTests;

public sealed class IntegrationTestDataBuilder(SchoolErpDbContext context)
{
    public async Task<Tenant> CreateTenantAsync(string? name = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var tenant = Tenant.Create(name ?? "Integration Tenant", $"integration-{suffix}");
        tenant.Activate(DateTime.UtcNow);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    public async Task<School> CreateSchoolAsync(Tenant tenant, string? code = null)
    {
        var school = School.Create(tenant, "Integration School", code ?? $"S{Guid.NewGuid():N}"[..12]);
        school.Activate(DateTime.UtcNow);
        context.Schools.Add(school);
        await context.SaveChangesAsync();
        return school;
    }

    public async Task<Campus> CreateCampusAsync(School school, string? code = null, bool isMain = false)
    {
        var campus = Campus.Create(school, "Integration Campus", code ?? $"C{Guid.NewGuid():N}"[..12], isMain);
        context.Campuses.Add(campus);
        await context.SaveChangesAsync();
        return campus;
    }

    public async Task<ApplicationRole> CreateRoleAsync(Tenant tenant, RoleType type = RoleType.School,
        string? code = null)
    {
        var role = ApplicationRole.CreateTenant(tenant.Id, code ?? $"TEST_{Guid.NewGuid():N}"[..20],
            "Integration role", type);
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    public async Task<ApplicationUser> CreateUserAsync(Tenant? tenant = null, School? school = null,
        Campus? campus = null, ApplicationRole? role = null)
    {
        var email = $"integration-{Guid.NewGuid():N}@example.test";
        var user = ApplicationUser.Create("Integration", "User", email, tenant?.Id, school?.Id, campus?.Id,
            activate: true, emailConfirmed: true);
        user.NormalizedEmail = email.ToUpperInvariant();
        user.NormalizedUserName = email.ToUpperInvariant();
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, "Valid#Pass1");
        context.Users.Add(user);
        if (role is not null)
            context.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
        await context.SaveChangesAsync();
        return user;
    }

    public async Task GrantPermissionAsync(ApplicationRole role, string permissionCode)
    {
        var permission = await context.Permissions.SingleAsync(x => x.Code == permissionCode);
        if (!await context.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id))
        {
            context.RolePermissions.Add(RolePermission.Create(role.Id, permission.Id));
            await context.SaveChangesAsync();
        }
    }
}
