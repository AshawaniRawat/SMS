using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Security;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Seed;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PermissionAuthorizationApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Database_grants_change_authorization_on_next_request_without_new_JWT()
    {
        Guid tenantId; Guid otherTenantId; Guid roleId; Guid permissionId; Guid userId; string email;
        await using (var context = database.CreateContext(platformAccess: true))
        {
            await new PermissionSeeder(context).SeedAsync();
            var tenant = Tenant.Create("Authorization Tenant", $"authorization-{Guid.NewGuid():N}"); tenant.Activate(DateTime.UtcNow);
            var other = Tenant.Create("Other Authorization Tenant", $"other-authorization-{Guid.NewGuid():N}"); other.Activate(DateTime.UtcNow);
            context.Tenants.AddRange(tenant, other); await context.SaveChangesAsync(); tenantId = tenant.Id; otherTenantId = other.Id;
            var role = ApplicationRole.CreateTenant(tenant.Id, "ROLE_READER", "Role Reader", RoleType.School);
            context.Roles.Add(role); await context.SaveChangesAsync(); roleId = role.Id;
            permissionId = context.Permissions.Single(x => x.Code == PermissionCatalog.Role.View).Id;
            email = $"authorization-{Guid.NewGuid():N}@example.com";
            var user = ApplicationUser.Create("Authorization", "User", email, tenant.Id, activate: true); userId = user.Id;
            using var manager = Manager(context); Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
            context.Set<IdentityUserRole<Guid>>().Add(new() { UserId = user.Id, RoleId = role.Id }); await context.SaveChangesAsync();
        }
        await using var factory = new ApiFactory(database.ConnectionString); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles")).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Valid#Pass1"));
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles")).StatusCode);
        await using (var context = database.CreateContext(platformAccess: true))
        { context.RolePermissions.Add(RolePermission.Create(roleId, permissionId)); await context.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/tenants/{otherTenantId}/roles")).StatusCode);
        await using (var context = database.CreateContext(platformAccess: true))
        { context.RolePermissions.Remove(context.RolePermissions.Single(x => x.RoleId == roleId && x.PermissionId == permissionId)); await context.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles")).StatusCode);
        await using (var context = database.CreateContext(platformAccess: true))
        { context.RolePermissions.Add(RolePermission.Create(roleId, permissionId)); var role = context.Roles.Single(x => x.Id == roleId); role.Deactivate(); await context.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles")).StatusCode);
    }

    private static UserManager<ApplicationUser> Manager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions()); options.Value.User.RequireUniqueEmail = true;
        return new(new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context), options,
            new PasswordHasher<ApplicationUser>(), [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
