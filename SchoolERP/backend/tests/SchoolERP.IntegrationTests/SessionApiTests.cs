using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SessionApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Logout_is_idempotent_and_revoked_session_cannot_refresh()
    {
        var email = await CreateUser("api-logout");
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var login = await Login(client, email);
        var body = new LogoutRequest(login.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout",
            new LogoutRequest("unknown-token"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = login.RefreshToken })).StatusCode);
    }

    [PostgresFact]
    public async Task Authenticated_user_can_list_and_revoke_only_owned_sessions_and_logout_all()
    {
        var emailA = await CreateUser("api-a");
        var emailB = await CreateUser("api-b");
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var a1 = await Login(client, emailA);
        var a2 = await Login(client, emailA);
        var b = await Login(client, emailB);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a1.AccessToken);

        var response = await client.GetAsync("/api/v1/auth/sessions?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("tokenHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        using var sessions = JsonDocument.Parse(json);
        Assert.Equal(2, sessions.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32());

        string bSession;
        await using (var context = database.CreateContext(platformAccess: true))
            bSession = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(
                context.RefreshTokens.Where(x => x.TokenHash == new RefreshTokenHasher().Hash(b.RefreshToken))
                    .Select(x => x.SessionId));
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsync($"/api/v1/auth/sessions/{bSession}/revoke", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout-all", null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = a1.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = a2.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = b.RefreshToken })).StatusCode);
    }

    private async Task<string> CreateUser(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        await using var context = database.CreateContext(platformAccess: true);
        using var manager = Manager(context);
        Assert.True((await manager.CreateAsync(ApplicationUser.Create("API", "Session", email, activate: true),
            "Valid#Pass1")).Succeeded);
        return email;
    }

    private static async Task<LoginTokens> Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Valid#Pass1"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        return new(data.GetProperty("accessToken").GetString()!, data.GetProperty("refreshToken").GetString()!);
    }

    private static UserManager<ApplicationUser> Manager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions()); options.Value.User.RequireUniqueEmail = true;
        var store = new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new UserManager<ApplicationUser>(store, options, new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private sealed record LoginTokens(string AccessToken, string RefreshToken);
}
