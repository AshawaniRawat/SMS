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
using SchoolERP.Application.Features.Identity;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class EmailVerificationApiTests(PostgresFixture database)
{
    [Fact]
    public async Task Verification_endpoints_are_rate_limited()
    {
        var fake = new FakeEmailService();
        await using var factory = Factory(fake, databaseRequired: false);
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/resend-email-verification",
                new ResendEmailVerificationRequest("invalid"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/api/v1/auth/resend-email-verification",
                new ResendEmailVerificationRequest("invalid"))).StatusCode);
        for (var attempt = 0; attempt < 20; attempt++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/confirm-email",
                new ConfirmEmailRequest(Guid.Empty, ""))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/auth/confirm-email",
            new ConfirmEmailRequest(Guid.Empty, ""))).StatusCode);
    }

    [PostgresFact]
    public async Task Resend_is_enumeration_safe_and_confirm_is_idempotent()
    {
        var user = await CreateUser("verify");
        var confirmed = await CreateUser("confirmed", emailConfirmed: true);
        var fake = new FakeEmailService();
        await using var factory = Factory(fake);
        using var client = factory.CreateClient();

        var known = await Resend(client, user.Email!);
        var unknown = await Resend(client, $"unknown-{Guid.NewGuid():N}@example.com");
        var already = await Resend(client, confirmed.Email!);
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.Equal(await known.Content.ReadAsStringAsync(), await already.Content.ReadAsStringAsync());
        Assert.Single(fake.Verifications);

        var request = RequestFromLink(fake.Verifications.Single().Link);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/v1/auth/confirm-email", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/v1/auth/confirm-email", request)).StatusCode);
        await using var context = database.CreateContext(platformAccess: true);
        var stored = await context.Users.SingleAsync(x => x.Id == user.Id);
        Assert.True(stored.EmailConfirmed);
        Assert.Equal(UserAccountStatus.Pending, stored.Status);
        Assert.False(stored.IsActive);
    }

    [PostgresFact]
    public async Task Registration_persists_unconfirmed_user_and_attempts_verification_without_exposing_token()
    {
        var fake = new FakeEmailService();
        await using var factory = Factory(fake);
        using var client = factory.CreateClient();
        var email = $"registration-verify-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/v1/platform/users",
            new CreatePlatformUserRequest("Registration", "Verify", email, "Valid#Pass1"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.Single(fake.Verifications);
        Assert.Equal(email, fake.Verifications[0].Recipient);
        await using var context = database.CreateContext(platformAccess: true);
        Assert.False((await context.Users.SingleAsync(x => x.Email == email)).EmailConfirmed);
    }

    [PostgresFact]
    public async Task Invalid_token_does_not_confirm_and_suspended_user_is_not_reactivated()
    {
        var unconfirmed = await CreateUser("invalid");
        var suspended = await CreateUser("suspended", activate: true);
        await using (var context = database.CreateContext(platformAccess: true))
        {
            var stored = await context.Users.SingleAsync(x => x.Id == suspended.Id);
            stored.Suspend(); await context.SaveChangesAsync();
        }
        var fake = new FakeEmailService();
        await using var factory = Factory(fake);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/confirm-email",
            new ConfirmEmailRequest(unconfirmed.Id, "malformed"))).StatusCode);
        await Resend(client, suspended.Email!);
        var request = RequestFromLink(fake.Verifications.Single().Link);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/confirm-email", request)).StatusCode);

        await using var verify = database.CreateContext(platformAccess: true);
        Assert.False((await verify.Users.SingleAsync(x => x.Id == unconfirmed.Id)).EmailConfirmed);
        var storedSuspended = await verify.Users.SingleAsync(x => x.Id == suspended.Id);
        Assert.True(storedSuspended.EmailConfirmed);
        Assert.Equal(UserAccountStatus.Suspended, storedSuspended.Status);
        Assert.False(storedSuspended.IsActive);
    }

    private ApiFactory Factory(FakeEmailService fake, bool databaseRequired = true) => new(
        databaseRequired ? database.ConnectionString : null, configureServices: services =>
        {
            services.RemoveAll<IEmailService>(); services.AddSingleton<IEmailService>(fake);
        });

    private async Task<ApplicationUser> CreateUser(string prefix, bool activate = false, bool emailConfirmed = false)
    {
        await using var context = database.CreateContext(platformAccess: true);
        using var manager = Manager(context);
        var user = ApplicationUser.Create("Email", "Verification", $"{prefix}-{Guid.NewGuid():N}@example.com",
            activate: activate, emailConfirmed: emailConfirmed);
        Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        return user;
    }

    private static Task<HttpResponseMessage> Resend(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/v1/auth/resend-email-verification", new ResendEmailVerificationRequest(email));

    private static ConfirmEmailRequest RequestFromLink(string link)
    {
        var query = new Uri(link).Query.TrimStart('?').Split('&').Select(x => x.Split('=', 2))
            .ToDictionary(x => Uri.UnescapeDataString(x[0]), x => Uri.UnescapeDataString(x[1]));
        return new ConfirmEmailRequest(Guid.Parse(query["userId"]), query["token"]);
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
        public List<(string Recipient, string Link)> Verifications { get; } = [];
        public Task SendPasswordResetEmailAsync(string email, string resetLink,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendEmailVerificationAsync(string email, string verificationLink,
            CancellationToken cancellationToken = default)
        {
            Verifications.Add((email, verificationLink)); return Task.CompletedTask;
        }
    }
}
