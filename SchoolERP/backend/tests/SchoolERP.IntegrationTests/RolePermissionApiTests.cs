using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Features.RolePermissions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Security;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Seed;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RolePermissionApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task API_is_tenant_safe_supports_bulk_and_protects_system_roles()
    {
        Guid tenantId; Guid otherId; Guid roleId; Guid permissionId; Guid systemRoleId; string email;
        await using (var context = database.CreateContext(platformAccess: true))
        {
            await new PermissionSeeder(context).SeedAsync(); await new RolePermissionSeeder(context).SeedAsync();
            var tenant = Tenant.Create("RP API", $"rp-api-{Guid.NewGuid():N}"); tenant.Activate(DateTime.UtcNow);
            var other = Tenant.Create("RP Other", $"rp-other-{Guid.NewGuid():N}"); other.Activate(DateTime.UtcNow);
            context.Tenants.AddRange(tenant, other); await context.SaveChangesAsync(); tenantId = tenant.Id; otherId = other.Id;
            var role = ApplicationRole.CreateTenant(tenant.Id, "API_HELPER", "API Helper", RoleType.School);
            context.Roles.Add(role); await context.SaveChangesAsync(); roleId = role.Id;
            permissionId = context.Permissions.Single(x => x.Code == PermissionCatalog.Student.View).Id;
            systemRoleId = context.Roles.Single(x => x.Code == SystemRoleCodes.SchoolAdmin).Id;
            email = $"rp-api-{Guid.NewGuid():N}@example.com";
            using var manager = Manager(context);
            Assert.True((await manager.CreateAsync(ApplicationUser.Create("Role", "Editor", email, tenant.Id, activate: true), "Valid#Pass1")).Succeeded);
        }
        await using var factory = new ApiFactory(database.ConnectionString); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles/{roleId}/permissions")).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Valid#Pass1"));
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            json.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/roles/{roleId}/permissions", new SetRolePermissionsRequest([permissionId]))).StatusCode);
        var get = await client.GetAsync($"/api/v1/tenants/{tenantId}/roles/{roleId}/permissions");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode); Assert.Contains("Student.View", await get.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/tenants/{otherId}/roles/{roleId}/permissions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/roles/{systemRoleId}/permissions", new SetRolePermissionsRequest([]))).StatusCode);
    }

    private static UserManager<ApplicationUser> Manager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions()); options.Value.User.RequireUniqueEmail = true;
        return new(new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context), options,
            new PasswordHasher<ApplicationUser>(), [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
