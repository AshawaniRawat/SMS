using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Application.Security;
using SchoolERP.Application.Abstractions;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AuthenticationTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Valid_platform_login_updates_last_login_and_returns_token_pair()
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
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.DoesNotContain(result.Value.RefreshToken!, await context.RefreshTokens.Select(x => x.TokenHash).FirstAsync());
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

    [PostgresFact]
    public async Task Refresh_rotates_token_and_reuse_revokes_the_session()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = CreateManager(context);
        var user = ApplicationUser.Create("Refresh", "Test", $"refresh-{Guid.NewGuid():N}@example.com", activate: true);
        Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        var service = CreateService(manager, context);
        var login = await service.AuthenticateAsync(new(user.Email!, "Valid#Pass1"));

        var refreshService = CreateRefreshService(manager, context);
        var rotated = await refreshService.RefreshAsync(login.Value!.RefreshToken!);
        Assert.True(rotated.IsSuccess, rotated.Error);
        Assert.NotEqual(login.Value.RefreshToken, rotated.Value!.RefreshToken);

        var replay = await refreshService.RefreshAsync(login.Value.RefreshToken!);
        Assert.Equal(RefreshTokenErrorCodes.ReuseDetected, replay.ErrorCode);
        Assert.All(await context.RefreshTokens.Where(x => x.UserId == user.Id).ToListAsync(),
            token => Assert.True(token.IsRevoked));
    }

    [PostgresFact]
    public async Task Concurrent_refresh_allows_at_most_one_success()
    {
        string rawToken;
        await using (var setupContext = fixture.CreateContext(platformAccess: true))
        {
            using var setupManager = CreateManager(setupContext);
            var user = ApplicationUser.Create("Concurrent", "Refresh", $"concurrent-{Guid.NewGuid():N}@example.com", activate: true);
            Assert.True((await setupManager.CreateAsync(user, "Valid#Pass1")).Succeeded);
            rawToken = (await CreateService(setupManager, setupContext)
                .AuthenticateAsync(new(user.Email!, "Valid#Pass1"))).Value!.RefreshToken!;
        }

        async Task<bool> RotateAsync()
        {
            await using var isolatedContext = fixture.CreateContext(platformAccess: true);
            using var isolatedManager = CreateManager(isolatedContext);
            return (await CreateRefreshService(isolatedManager, isolatedContext).RefreshAsync(rawToken)).IsSuccess;
        }

        var results = await Task.WhenAll(RotateAsync(), RotateAsync());
        Assert.Equal(1, results.Count(x => x));
    }

    private static AuthenticationService CreateService(UserManager<ApplicationUser> manager, SchoolErpDbContext context,
        bool requireConfirmedEmail = false)
    {
        var identityOptions = Options.Create(new IdentitySettings { RequireConfirmedEmail = requireConfirmedEmail });
        var access = new JwtAccessTokenService(Options.Create(new JwtOptions
        {
            Issuer = "SchoolERP.Tests", Audience = "SchoolERP.Tests.Client",
            SigningKey = "test-only-signing-key-that-is-at-least-32-bytes-long"
        }), TimeProvider.System);
        var refresh = new RefreshTokenService(context, manager, new RefreshTokenHasher(), access, new TestClientContext(),
            Options.Create(new RefreshTokenOptions()), identityOptions, TimeProvider.System,
            NullLogger<RefreshTokenService>.Instance);
        return new AuthenticationService(manager, context, new LoginRequestValidator(), identityOptions,
            TimeProvider.System, access, refresh, NullLogger<AuthenticationService>.Instance);
    }

    private static RefreshTokenService CreateRefreshService(UserManager<ApplicationUser> manager,
        SchoolErpDbContext context)
    {
        var access = new JwtAccessTokenService(Options.Create(new JwtOptions
        {
            Issuer = "SchoolERP.Tests", Audience = "SchoolERP.Tests.Client",
            SigningKey = "test-only-signing-key-that-is-at-least-32-bytes-long"
        }), TimeProvider.System);
        return new RefreshTokenService(context, manager, new RefreshTokenHasher(), access, new TestClientContext(),
            Options.Create(new RefreshTokenOptions()), Options.Create(new IdentitySettings()), TimeProvider.System,
            NullLogger<RefreshTokenService>.Instance);
    }

    private sealed class TestClientContext : IClientRequestContext
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "integration-test";
    }

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
