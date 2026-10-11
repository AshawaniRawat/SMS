using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Security;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SessionRevocationTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Logout_revokes_full_rotated_chain_and_is_idempotent_for_used_and_unknown_tokens()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = Manager(context);
        var user = await CreateUser(manager, "logout");
        var original = await Login(manager, context, user);
        var refresh = RefreshService(manager, context);
        var rotated = await refresh.RefreshAsync(original);
        Assert.True(rotated.IsSuccess);

        var sessions = SessionService(context);
        Assert.True((await sessions.LogoutSessionAsync(original)).IsSuccess);
        Assert.True((await sessions.LogoutSessionAsync(original)).IsSuccess);
        Assert.True((await sessions.LogoutSessionAsync("unknown-token-value")).IsSuccess);
        Assert.All(await context.RefreshTokens.Where(x => x.UserId == user.Id).ToListAsync(),
            token => Assert.True(token.IsRevoked));
        Assert.False((await refresh.RefreshAsync(rotated.Value!.RefreshToken)).IsSuccess);
    }

    [PostgresFact]
    public async Task Logout_all_lists_aggregated_sessions_and_owned_revocation_does_not_cross_users()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = Manager(context);
        var userA = await CreateUser(manager, "sessions-a");
        var userB = await CreateUser(manager, "sessions-b");
        var tokenA1 = await Login(manager, context, userA);
        var tokenA2 = await Login(manager, context, userA);
        var tokenB = await Login(manager, context, userB);
        var service = SessionService(context);

        var listed = await service.GetSessionsAsync(userA.Id, new PagedRequest(1, 100));
        Assert.Equal(2, listed.TotalCount);
        Assert.All(listed.Items, session => Assert.True(session.IsActive));
        var serialized = System.Text.Json.JsonSerializer.Serialize(listed);
        Assert.DoesNotContain("tokenHash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("parentToken", serialized, StringComparison.OrdinalIgnoreCase);

        var userBSession = await context.RefreshTokens.Where(x => x.TokenHash == new RefreshTokenHasher().Hash(tokenB))
            .Select(x => x.SessionId).SingleAsync();
        var denied = await service.RevokeSessionByIdAsync(userA.Id, userBSession);
        Assert.Equal(SessionErrorCodes.SessionNotFound, denied.ErrorCode);
        Assert.True((await RefreshService(manager, context).RefreshAsync(tokenB)).IsSuccess);

        Assert.True((await service.LogoutAllAsync(userA.Id)).IsSuccess);
        Assert.False((await RefreshService(manager, context).RefreshAsync(tokenA1)).IsSuccess);
        Assert.False((await RefreshService(manager, context).RefreshAsync(tokenA2)).IsSuccess);
    }

    [PostgresFact]
    public async Task Suspended_user_can_logout()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = Manager(context);
        var user = await CreateUser(manager, "suspended-logout");
        var token = await Login(manager, context, user);
        user.Suspend();
        Assert.True((await manager.UpdateAsync(user)).Succeeded);
        Assert.True((await SessionService(context).LogoutSessionAsync(token)).IsSuccess);
        Assert.False((await RefreshService(manager, context).RefreshAsync(token)).IsSuccess);
    }

    [PostgresFact]
    public async Task Refresh_and_logout_race_leaves_no_usable_session()
    {
        Guid userId;
        string token;
        await using (var setup = fixture.CreateContext(platformAccess: true))
        {
            using var manager = Manager(setup);
            var user = await CreateUser(manager, "refresh-logout-race");
            userId = user.Id;
            token = await Login(manager, setup, user);
        }

        async Task RefreshAsync()
        {
            await using var context = fixture.CreateContext(platformAccess: true);
            using var manager = Manager(context);
            await RefreshService(manager, context).RefreshAsync(token);
        }
        async Task LogoutAsync()
        {
            await using var context = fixture.CreateContext(platformAccess: true);
            await SessionService(context).LogoutSessionAsync(token);
        }

        await Task.WhenAll(RefreshAsync(), LogoutAsync());
        await using var verify = fixture.CreateContext(platformAccess: true);
        var now = DateTime.UtcNow;
        Assert.False(await verify.RefreshTokens.AnyAsync(x => x.UserId == userId && !x.IsUsed &&
            x.RevokedAtUtc == null && x.ExpiresAtUtc > now));
    }

    [PostgresFact]
    public async Task Logout_all_and_refresh_race_leaves_no_active_user_session()
    {
        Guid userId;
        string token;
        await using (var setup = fixture.CreateContext(platformAccess: true))
        {
            using var manager = Manager(setup);
            var user = await CreateUser(manager, "all-refresh-race");
            userId = user.Id;
            token = await Login(manager, setup, user);
        }

        async Task RefreshAsync()
        {
            await using var context = fixture.CreateContext(platformAccess: true);
            using var manager = Manager(context);
            await RefreshService(manager, context).RefreshAsync(token);
        }
        async Task LogoutAllAsync()
        {
            await using var context = fixture.CreateContext(platformAccess: true);
            await SessionService(context).LogoutAllAsync(userId);
        }

        await Task.WhenAll(RefreshAsync(), LogoutAllAsync());
        await using var verify = fixture.CreateContext(platformAccess: true);
        var now = DateTime.UtcNow;
        Assert.False(await verify.RefreshTokens.AnyAsync(x => x.UserId == userId && !x.IsUsed &&
            x.RevokedAtUtc == null && x.ExpiresAtUtc > now));
    }

    private static async Task<ApplicationUser> CreateUser(UserManager<ApplicationUser> manager, string prefix)
    {
        var user = ApplicationUser.Create("Session", "Test", $"{prefix}-{Guid.NewGuid():N}@example.com", activate: true);
        Assert.True((await manager.CreateAsync(user, "Valid#Pass1")).Succeeded);
        return user;
    }

    private static async Task<string> Login(UserManager<ApplicationUser> manager, SchoolErpDbContext context,
        ApplicationUser user) => (await AuthenticationService(manager, context)
        .AuthenticateAsync(new(user.Email!, "Valid#Pass1"))).Value!.RefreshToken!;

    private static UserSessionService SessionService(SchoolErpDbContext context) => new(context,
        new RefreshTokenHasher(), new ClientContext(), TimeProvider.System, NullLogger<UserSessionService>.Instance);

    private static RefreshTokenService RefreshService(UserManager<ApplicationUser> manager, SchoolErpDbContext context)
        => new(context, manager, new RefreshTokenHasher(), AccessTokenService(), new ClientContext(),
            Options.Create(new RefreshTokenOptions()), Options.Create(new IdentitySettings()), TimeProvider.System,
            NullLogger<RefreshTokenService>.Instance);

    private static AuthenticationService AuthenticationService(UserManager<ApplicationUser> manager,
        SchoolErpDbContext context) => new(manager, context, new LoginRequestValidator(),
        Options.Create(new IdentitySettings()), TimeProvider.System, AccessTokenService(), RefreshService(manager, context),
        NullLogger<AuthenticationService>.Instance);

    private static JwtAccessTokenService AccessTokenService() => new(Options.Create(new JwtOptions
    {
        Issuer = "SchoolERP.Tests", Audience = "SchoolERP.Tests.Client",
        SigningKey = "test-only-signing-key-that-is-at-least-32-bytes-long"
    }), TimeProvider.System);

    private static UserManager<ApplicationUser> Manager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions());
        options.Value.User.RequireUniqueEmail = true;
        var store = new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new UserManager<ApplicationUser>(store, options, new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private sealed class ClientContext : IClientRequestContext
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "session-integration-test";
    }
}
