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
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Seed;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PermissionApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Read_API_is_authenticated_filterable_and_has_no_mutation_endpoint()
    {
        string email;
        await using (var context = database.CreateContext(platformAccess: true))
        {
            await new PermissionSeeder(context).SeedAsync();
            await new RolePermissionSeeder(context).SeedAsync();
            var tenant = Tenant.Create("Permission API Tenant", $"permission-api-{Guid.NewGuid():N}");
            tenant.Activate(DateTime.UtcNow); context.Tenants.Add(tenant); await context.SaveChangesAsync();
            email = $"permission-api-{Guid.NewGuid():N}@example.com";
            using var manager = Manager(context);
            var user = ApplicationUser.Create("Permission", "Reader", email, tenant.Id, activate: true);
            Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
            Assert.True((await manager.AddToRoleAsync(user, SystemRoleCodes.SchoolAdmin)).Succeeded);
        }

        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/permissions")).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Valid#Pass1"));
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            loginJson.RootElement.GetProperty("data").GetProperty("accessToken").GetString());

        var list = await client.GetAsync("/api/v1/permissions?module=Student&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var items = listJson.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToArray();
        Assert.NotEmpty(items);
        Assert.All(items, x => Assert.Equal("Student", x.GetProperty("module").GetString()));

        var search = await client.GetAsync("/api/v1/permissions?search=refund");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Contains("Fee.Refund", await search.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync("/api/v1/permissions/by-code?code=Fee.Refund")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/permissions/modules")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed,
            (await client.PostAsJsonAsync("/api/v1/permissions", new { code = "Custom.Use" })).StatusCode);
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
