using System.Net;
using System.Net.Http.Json;
using SchoolERP.Application.Features.Identity;
using SchoolERP.Application.Features.Tenants;
using Xunit;
using SchoolERP.Domain.Entities;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class UserRegistrationApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Platform_registration_returns_safe_201_and_duplicate_returns_409()
    {
        await using var factory = new ApiFactory(database.ConnectionString, authorizePlatform: true);
        using var client = factory.CreateClient();
        var email = $"platform-{Guid.NewGuid():N}@example.com";
        var request = new CreatePlatformUserRequest("Platform", "Admin", email, "Valid#Pass1", "Platform Admin", "+123");
        var created = await client.PostAsJsonAsync("/api/v1/platform/users", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/platform/users", request with { Email = email.ToUpperInvariant() })).StatusCode);
    }

    [PostgresFact]
    public async Task Tenant_registration_uses_route_scope_and_returns_not_found_for_unknown_tenant()
    {
        var tenant = Tenant.Create("Registration Tenant", $"registration-{Guid.NewGuid():N}");
        await using (var context = database.CreateContext(platformAccess: true))
        { context.Tenants.Add(tenant); await context.SaveChangesAsync(); }
        await using var factory = new ApiFactory(database.ConnectionString, tenantId: tenant.Id);
        using var client = factory.CreateClient();
        var request = new CreateTenantUserApiRequest("Tenant", "User", $"tenant-{Guid.NewGuid():N}@example.com", "Valid#Pass1");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/v1/tenants/{tenant.Id}/users", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/tenants/{Guid.NewGuid()}/users", request with { Email = $"missing-{Guid.NewGuid():N}@example.com" })).StatusCode);
    }
}
