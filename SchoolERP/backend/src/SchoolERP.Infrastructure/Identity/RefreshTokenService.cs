using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Security;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;

namespace SchoolERP.Infrastructure.Identity;

public sealed class RefreshTokenService(
    SchoolErpDbContext context,
    UserManager<ApplicationUser> userManager,
    IRefreshTokenHasher hasher,
    IAccessTokenService accessTokenService,
    IClientRequestContext client,
    IOptions<RefreshTokenOptions> options,
    IOptions<IdentitySettings> identitySettings,
    TimeProvider timeProvider,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    public async Task<RefreshTokenIssueResult> CreateAsync(AuthenticatedUserDto user, string securityStamp,
        CancellationToken cancellationToken = default)
    {
        var raw = hasher.GenerateToken();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddDays(options.Value.RefreshTokenLifetimeDays);
        var sessionId = Guid.NewGuid().ToString("N");
        context.RefreshTokens.Add(RefreshToken.Create(user.UserId, hasher.Hash(raw), expires, sessionId,
            hasher.Hash(securityStamp), user.TenantId, user.SchoolId, user.CampusId, client.IpAddress, client.UserAgent));
        await context.SaveChangesAsync(cancellationToken);
        return new(raw, expires, sessionId);
    }

    public async Task<Result<TokenPairResponse>> RefreshAsync(string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 1024)
            return Fail("The refresh request is invalid.", RefreshTokenErrorCodes.InvalidRequest);

        var hash = hasher.Hash(refreshToken);
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var current = await context.RefreshTokens
                .FromSqlInterpolated($"SELECT * FROM refresh_tokens WHERE token_hash = {hash} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (current is null)
                return Fail("The refresh token is invalid.", RefreshTokenErrorCodes.Invalid);

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (current.IsUsed || (current.IsRevoked && current.ReplacedByTokenId.HasValue))
            {
                await RevokeSessionCoreAsync(current.UserId, current.SessionId, now, "RefreshTokenReuseDetected", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogWarning("RefreshTokenReuseDetected for UserId {UserId}, SessionId {SessionId}",
                    current.UserId, current.SessionId);
                return Fail("The refresh token is no longer valid.", RefreshTokenErrorCodes.ReuseDetected);
            }
            if (current.IsRevoked)
                return Fail("The refresh token has been revoked.", RefreshTokenErrorCodes.Revoked);
            if (current.IsExpired(now))
                return Fail("The refresh token has expired.", RefreshTokenErrorCodes.Expired);

            var user = await userManager.FindByIdAsync(current.UserId.ToString());
            if (user is null || !await IsEligibleAsync(user, cancellationToken) ||
                !FixedTimeEquals(current.SecurityStampHash, hasher.Hash(user.SecurityStamp ?? string.Empty)))
            {
                await RevokeSessionCoreAsync(current.UserId, current.SessionId, now, "AccountOrSecurityStampChanged", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogWarning("RefreshRejectedAccountState for UserId {UserId}, SessionId {SessionId}",
                    current.UserId, current.SessionId);
                return Fail("The account is unavailable.", RefreshTokenErrorCodes.AccountUnavailable);
            }

            var authenticatedUser = Map(user);
            var rawReplacement = hasher.GenerateToken();
            var replacement = RefreshToken.Create(user.Id, hasher.Hash(rawReplacement),
                now.AddDays(options.Value.RefreshTokenLifetimeDays), current.SessionId,
                hasher.Hash(user.SecurityStamp ?? string.Empty), user.TenantId, user.SchoolId, user.CampusId,
                client.IpAddress, client.UserAgent, current.Id);
            current.MarkUsed(now, replacement.Id);
            context.RefreshTokens.Add(replacement);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var access = accessTokenService.GenerateToken(authenticatedUser);
            logger.LogInformation("RefreshTokenRotated for UserId {UserId}, SessionId {SessionId}", user.Id, current.SessionId);
            return Result<TokenPairResponse>.Ok(new(access.AccessToken, access.ExpiresAtUtc, rawReplacement,
                replacement.ExpiresAtUtc, access.TokenType, authenticatedUser));
        });
    }

    public async Task<Result<bool>> RevokeAsync(string refreshToken, string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return Result<bool>.Ok(false);
        var token = await context.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hasher.Hash(refreshToken), cancellationToken);
        if (token is null) return Result<bool>.Ok(false);
        token.Revoke(timeProvider.GetUtcNow().UtcDateTime, reason, client.IpAddress);
        await context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RevokeSessionAsync(Guid userId, string sessionId, string reason,
        CancellationToken cancellationToken = default)
    {
        var count = await RevokeSessionCoreAsync(userId, sessionId, timeProvider.GetUtcNow().UtcDateTime, reason, cancellationToken);
        return Result<bool>.Ok(count > 0);
    }

    private async Task<int> RevokeSessionCoreAsync(Guid userId, string sessionId, DateTime now, string reason,
        CancellationToken cancellationToken)
    {
        var tokens = await context.RefreshTokens
            .Where(x => x.UserId == userId && x.SessionId == sessionId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens) token.Revoke(now, reason, client.IpAddress);
        if (tokens.Count > 0) await context.SaveChangesAsync(cancellationToken);
        return tokens.Count;
    }

    private async Task<bool> IsEligibleAsync(ApplicationUser user, CancellationToken ct)
    {
        if (!user.IsActive || user.Status != UserAccountStatus.Active ||
            (identitySettings.Value.RequireConfirmedEmail && !user.EmailConfirmed)) return false;
        if (!user.TenantId.HasValue) return !user.SchoolId.HasValue && !user.CampusId.HasValue;
        if (user.CampusId.HasValue && !user.SchoolId.HasValue) return false;
        var tenantActive = await context.Tenants.AsNoTracking().AnyAsync(
            x => x.Id == user.TenantId && x.Status == TenantStatus.Active, ct);
        if (!tenantActive) return false;
        if (user.SchoolId.HasValue)
        {
            var schoolActive = await context.Schools.AsNoTracking().IgnoreQueryFilters().AnyAsync(
                x => x.Id == user.SchoolId && x.TenantId == user.TenantId && x.Status == SchoolStatus.Active, ct);
            if (!schoolActive) return false;
        }
        return !user.CampusId.HasValue || await context.Campuses.AsNoTracking().IgnoreQueryFilters().AnyAsync(
            x => x.Id == user.CampusId && x.TenantId == user.TenantId && x.SchoolId == user.SchoolId &&
                 x.Status == CampusStatus.Active, ct);
    }

    private static bool FixedTimeEquals(string left, string right) => CryptographicOperations.FixedTimeEquals(
        Convert.FromHexString(left), Convert.FromHexString(right));
    private static AuthenticatedUserDto Map(ApplicationUser x) => new(x.Id, x.FirstName, x.LastName, x.Email!,
        x.DisplayName, x.TenantId, x.SchoolId, x.CampusId, x.Status, x.EmailConfirmed, x.TwoFactorEnabled, x.LastLoginAt);
    private static Result<TokenPairResponse> Fail(string message, string code) => Result<TokenPairResponse>.Fail(message, code);
}
