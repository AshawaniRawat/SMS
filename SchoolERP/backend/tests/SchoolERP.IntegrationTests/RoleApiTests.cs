using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Features.Roles;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;
using SchoolERP.Infrastructure.Persistence.Seed;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RoleApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Tenant_role_API_is_authenticated_scoped_and_supports_CRUD_lifecycle()
    {
        Guid tenantId; Guid otherTenantId; string email;
        await using (var context = database.CreateContext(platformAccess: true))
        {
            await new PermissionSeeder(context).SeedAsync(); await new RolePermissionSeeder(context).SeedAsync();
            var tenant = Tenant.Create("API Role Tenant", $"api-role-{Guid.NewGuid():N}"); tenant.Activate(DateTime.UtcNow);
            var other = Tenant.Create("Other Role Tenant", $"other-role-{Guid.NewGuid():N}"); other.Activate(DateTime.UtcNow);
            context.AddRange(tenant, other); await context.SaveChangesAsync();
            tenantId = tenant.Id; otherTenantId = other.Id; email = $"role-api-{Guid.NewGuid():N}@example.com";
            using var manager = Manager(context);
            var user = ApplicationUser.Create("Role", "Admin", email, tenantId, activate: true);
            Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
            Assert.True((await manager.AddToRoleAsync(user, SystemRoleCodes.SchoolAdmin)).Succeeded);
        }
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles")).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Valid#Pass1"));
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            loginJson.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        var createRequest = new CreateTenantRoleRequest("FEE_ASSISTANT", "Fee Assistant", "Assists finance", RoleType.Tenant);
        var created = await client.PostAsJsonAsync($"/api/v1/tenants/{tenantId}/roles", createRequest);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var roleId = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"/api/v1/tenants/{tenantId}/roles", createRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tenants/{tenantId}/roles/{roleId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/tenants/{otherTenantId}/roles/{roleId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/tenants/{tenantId}/roles/{roleId}",
            new UpdateRoleRequest("Senior Fee Assistant", "Updated", 50))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/v1/tenants/{tenantId}/roles/{roleId}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/v1/tenants/{tenantId}/roles/{roleId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/v1/tenants/{tenantId}/roles/{roleId}/archive", null)).StatusCode);
    }

    private static UserManager<ApplicationUser> Manager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions()); options.Value.User.RequireUniqueEmail = true;
        var store = new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new(store, options, new PasswordHasher<ApplicationUser>(), [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
