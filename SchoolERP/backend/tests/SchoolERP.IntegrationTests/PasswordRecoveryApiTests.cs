using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PasswordRecoveryApiTests(PostgresFixture database)
{
    [Fact]
    public async Task Recovery_endpoints_enforce_independent_IP_rate_limits()
    {
        var fake = new FakeEmailService();
        await using var factory = new ApiFactory(configureServices: services =>
        {
            services.RemoveAll<IEmailService>(); services.AddSingleton<IEmailService>(fake);
        });
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
                new ForgotPasswordRequest("invalid"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest("invalid"))).StatusCode);

        for (var attempt = 0; attempt < 10; attempt++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/reset-password",
                new ResetPasswordRequest("invalid", "", "", ""))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest("invalid", "", "", ""))).StatusCode);
    }

    [PostgresFact]
    public async Task Known_and_unknown_forgot_requests_are_publicly_identical_and_only_known_sends_email()
    {
        var email = await CreateUser("forgot");
        var fake = new FakeEmailService();
        await using var factory = Factory(fake);
        using var client = factory.CreateClient();

        var known = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email));
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest($"unknown-{Guid.NewGuid():N}@example.com"));
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.Single(fake.Messages);
        Assert.StartsWith("http://localhost:5173/reset-password?", fake.Messages[0].ResetLink);
    }

    [PostgresFact]
    public async Task Valid_reset_changes_password_is_one_time_and_revokes_all_refresh_sessions()
    {
        var email = await CreateUser("reset");
        var fake = new FakeEmailService();
        await using var factory = Factory(fake);
        using var client = factory.CreateClient();
        var firstLogin = await Login(client, email, "Valid#Pass1");
        var secondLogin = await Login(client, email, "Valid#Pass1");
        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email));
        var token = ParseQuery(fake.Messages.Single().ResetLink, "token");
        var request = new ResetPasswordRequest(email, token, "NewValid#Pass2", "NewValid#Pass2");

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", request);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        var responseBody = await reset.Content.ReadAsStringAsync();
        Assert.DoesNotContain(token, responseBody); Assert.DoesNotContain("accessToken", responseBody);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/v1/auth/reset-password", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "Valid#Pass1"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "NewValid#Pass2"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await Refresh(client, firstLogin.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, await Refresh(client, secondLogin.RefreshToken));

        await using var context = database.CreateContext(platformAccess: true);
        var changed = await context.Users.Where(x => x.Email == email).Select(x => x.PasswordChangedAt).SingleAsync();
        Assert.NotNull(changed);
        Assert.All(await context.RefreshTokens.Where(x => x.User.Email == email).ToListAsync(),
            tokenRow => Assert.True(tokenRow.IsRevoked));
    }

    [PostgresFact]
    public async Task Invalid_token_and_weak_password_fail_without_changing_existing_password()
    {
        var email = await CreateUser("invalid-reset");
        var fake = new FakeEmailService();
        await using var factory = Factory(fake);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email));
        var token = ParseQuery(fake.Messages.Single().ResetLink, "token");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest(email, "malformed", "NewValid#Pass2", "NewValid#Pass2"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest(email, token, "weak", "weak"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "Valid#Pass1"))).StatusCode);
    }

    private ApiFactory Factory(FakeEmailService fake) => new(database.ConnectionString, configureServices: services =>
    {
        services.RemoveAll<IEmailService>();
        services.AddSingleton<IEmailService>(fake);
    });

    private async Task<string> CreateUser(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        await using var context = database.CreateContext(platformAccess: true);
        using var manager = Manager(context);
        Assert.True((await manager.CreateAsync(ApplicationUser.Create("Password", "Recovery", email, activate: true),
            "Valid#Pass1")).Succeeded);
        return email;
    }

    private static async Task<LoginTokens> Login(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        return new(data.GetProperty("refreshToken").GetString()!);
    }

    private static async Task<HttpStatusCode> Refresh(HttpClient client, string token) =>
        (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = token })).StatusCode;

    private static string ParseQuery(string url, string key)
    {
        var query = new Uri(url).Query.TrimStart('?').Split('&')
            .Select(x => x.Split('=', 2)).ToDictionary(x => Uri.UnescapeDataString(x[0]), x => Uri.UnescapeDataString(x[1]));
        return query[key];
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

    private sealed class FakeEmailService : IEmailService
    {
        public List<(string Recipient, string ResetLink)> Messages { get; } = [];
        public Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken = default)
        {
            Messages.Add((email, resetLink)); return Task.CompletedTask;
        }
        public Task SendEmailVerificationAsync(string email, string verificationLink,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record LoginTokens(string RefreshToken);
}
