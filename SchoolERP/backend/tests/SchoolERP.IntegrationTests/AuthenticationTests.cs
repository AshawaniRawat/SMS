using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Application.Security;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AuthenticationTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Valid_platform_login_updates_last_login_and_returns_access_token_only()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = CreateManager(context);
        var user = ApplicationUser.Create("Active", "Platform", $"active-{Guid.NewGuid():N}@example.com", activate: true);
        Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        var result = await CreateService(manager, context).AuthenticateAsync(new(user.Email!, "Valid#Pass1"));
        Assert.True(result.IsSuccess, result.Error); Assert.False(result.Value!.RequiresTwoFactor);
        Assert.NotNull(result.Value.User.LastLoginAt); Assert.Null(result.Value.User.TenantId);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken)); Assert.Equal("Bearer", result.Value.TokenType);
        var serialized = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("refreshToken", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [PostgresFact]
    public async Task Wrong_password_increments_count_success_resets_it_and_unknown_user_is_generic()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = CreateManager(context);
        var user = ApplicationUser.Create("Counter", "Test", $"counter-{Guid.NewGuid():N}@example.com", activate: true);
        Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        var service = CreateService(manager, context);
        var wrong = await service.AuthenticateAsync(new(user.Email!, "Wrong#Pass1"));
        var unknown = await service.AuthenticateAsync(new($"unknown-{Guid.NewGuid():N}@example.com", "Wrong#Pass1"));
        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, wrong.ErrorCode);
        Assert.Equal(wrong.Error, unknown.Error);
        Assert.Equal(1, (await manager.FindByIdAsync(user.Id.ToString()))!.AccessFailedCount);
        Assert.True((await service.AuthenticateAsync(new(user.Email!, "Valid#Pass1"))).IsSuccess);
        Assert.Equal(0, (await manager.FindByIdAsync(user.Id.ToString()))!.AccessFailedCount);
    }

    [PostgresFact]
    public async Task Failed_attempt_threshold_locks_account_and_correct_password_remains_rejected()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = CreateManager(context);
        var user = ApplicationUser.Create("Locked", "Test", $"locked-{Guid.NewGuid():N}@example.com", activate: true);
        Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        var service = CreateService(manager, context);
        for (var attempt = 0; attempt < manager.Options.Lockout.MaxFailedAccessAttempts; attempt++)
            Assert.False((await service.AuthenticateAsync(new(user.Email!, "Wrong#Pass1"))).IsSuccess);
        var locked = await service.AuthenticateAsync(new(user.Email!, "Valid#Pass1"));
        Assert.Equal(AuthenticationErrorCodes.AccountLocked, locked.ErrorCode);
    }

    [PostgresFact]
    public async Task Two_factor_returns_challenge_state_without_finalizing_last_login()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = CreateManager(context);
        var user = ApplicationUser.Create("Two", "Factor", $"2fa-{Guid.NewGuid():N}@example.com", activate: true);
        Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        Assert.True((await manager.SetTwoFactorEnabledAsync(user, true)).Succeeded);
        var result = await CreateService(manager, context).AuthenticateAsync(new(user.Email!, "Valid#Pass1"));
        Assert.True(result.IsSuccess); Assert.True(result.Value!.RequiresTwoFactor); Assert.Null(result.Value.User.LastLoginAt);
        Assert.Null(result.Value.AccessToken);
    }

    private static AuthenticationService CreateService(UserManager<ApplicationUser> manager, SchoolErpDbContext context,
        bool requireConfirmedEmail = false) => new(manager, context, new LoginRequestValidator(),
        Options.Create(new IdentitySettings { RequireConfirmedEmail = requireConfirmedEmail }), TimeProvider.System,
        new JwtAccessTokenService(Options.Create(new JwtOptions
        {
            Issuer = "SchoolERP.Tests", Audience = "SchoolERP.Tests.Client",
            SigningKey = "test-only-signing-key-that-is-at-least-32-bytes-long"
        }), TimeProvider.System),
        NullLogger<AuthenticationService>.Instance);

    private static UserManager<ApplicationUser> CreateManager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions());
        options.Value.User.RequireUniqueEmail = true; options.Value.Lockout.AllowedForNewUsers = true;
        options.Value.Lockout.MaxFailedAccessAttempts = 5; options.Value.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        var store = new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new UserManager<ApplicationUser>(store, options, new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
