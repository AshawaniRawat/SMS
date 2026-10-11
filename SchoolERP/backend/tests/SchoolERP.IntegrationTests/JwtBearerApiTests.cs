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
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class JwtBearerApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Login_issues_bearer_token_and_me_requires_and_accepts_it()
    {
        var email = $"jwt-{Guid.NewGuid():N}@example.com";
        await CreateActiveUser(email);
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Valid#Pass1"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var document = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        var token = data.GetProperty("accessToken").GetString();
        Assert.Equal("Bearer", data.GetProperty("tokenType").GetString());
        Assert.True(data.TryGetProperty("refreshToken", out var refreshToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken.GetString()));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains(email, await me.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [PostgresFact]
    public async Task Tenant_A_token_cannot_read_Tenant_B_school()
    {
        Guid tenantAId; Guid schoolBId; var email = $"tenant-jwt-{Guid.NewGuid():N}@example.com";
        await using (var context = database.CreateContext(platformAccess: true))
        {
            var tenantA = Tenant.Create("JWT Tenant A", $"jwt-a-{Guid.NewGuid():N}"); tenantA.Activate(DateTime.UtcNow);
            var tenantB = Tenant.Create("JWT Tenant B", $"jwt-b-{Guid.NewGuid():N}"); tenantB.Activate(DateTime.UtcNow);
            var schoolB = School.Create(tenantB, "JWT School B", "JWT-B"); schoolB.Activate(DateTime.UtcNow);
            context.AddRange(tenantA, tenantB, schoolB); await context.SaveChangesAsync();
            tenantAId = tenantA.Id; schoolBId = schoolB.Id;
            using var manager = Manager(context);
            Assert.True((await manager.CreateAsync(ApplicationUser.Create("Tenant", "User", email, tenantA.Id, activate: true), "Valid#Pass1")).Succeeded);
        }
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Valid#Pass1"));
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/schools/{schoolBId}")).StatusCode);
        var me = await client.GetStringAsync("/api/v1/auth/me"); Assert.Contains(tenantAId.ToString(), me);
    }

    private async Task CreateActiveUser(string email)
    {
        await using var context = database.CreateContext(platformAccess: true); using var manager = Manager(context);
        Assert.True((await manager.CreateAsync(ApplicationUser.Create("JWT", "User", email, activate: true), "Valid#Pass1")).Succeeded);
    }
    private static UserManager<ApplicationUser> Manager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions()); options.Value.User.RequireUniqueEmail = true;
        var store = new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new UserManager<ApplicationUser>(store, options, new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
