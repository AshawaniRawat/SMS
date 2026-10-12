using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Security;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;
using SchoolERP.Application.Features.Audit;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Security;

namespace SchoolERP.Infrastructure.Identity;

public sealed class UserSessionService(
    SchoolErpDbContext context,
    IRefreshTokenHasher hasher,
    IClientRequestContext client,
    TimeProvider timeProvider,
    ILogger<UserSessionService> logger,
    IAuditService? audit = null) : ISessionService
{
    public async Task<Result<bool>> LogoutSessionAsync(string? refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 1024)
            return Result<bool>.Fail("A refresh token is required.", SessionErrorCodes.InvalidRequest);

        var hash = hasher.Hash(refreshToken);
        var identity = await context.RefreshTokens.AsNoTracking()
            .Where(x => x.TokenHash == hash)
            .Select(x => new { x.UserId, x.SessionId, x.User.TenantId, x.User.SchoolId, x.User.CampusId })
            .SingleOrDefaultAsync(cancellationToken);

        if (identity is null)
        {
            logger.LogInformation("LogoutIdempotent from IP {IpAddress}", client.IpAddress);
            return Result<bool>.Ok(false);
        }

        var strategy = context.Database.CreateExecutionStrategy();
        var count = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await SessionConcurrencyLock.AcquireAsync(context, identity.UserId, cancellationToken);
            var revoked = await RevokeAsync(
                x => x.UserId == identity.UserId && x.SessionId == identity.SessionId,
                SessionRevocationReasons.Logout,
                cancellationToken);
            if(revoked>0&&audit is not null)await audit.WriteAsync(new AuditEntry(AuditActions.Authentication.Logout,"Authentication","ApplicationUser",identity.UserId.ToString(),Outcome:AuditOutcome.Succeeded,Description:"User session logged out.",Metadata:new{reason=SessionRevocationReasons.Logout},TenantId:identity.TenantId,SchoolId:identity.SchoolId,CampusId:identity.CampusId,ActorUserId:identity.UserId,SessionId:identity.SessionId),cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return revoked;
        });

        logger.LogInformation(count > 0 ? "LogoutSucceeded for UserId {UserId}, SessionId {SessionId}, IP {IpAddress}"
                : "LogoutIdempotent for UserId {UserId}, SessionId {SessionId}, IP {IpAddress}",
            identity.UserId, identity.SessionId, client.IpAddress);
        return Result<bool>.Ok(count > 0);
    }

    public async Task<Result<bool>> LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
        => await RevokeAllAsync(userId, SessionRevocationReasons.LogoutAllDevices, "AllUserSessionsRevoked",
            cancellationToken);

    public async Task<Result<bool>> RevokeAllAfterPasswordResetAsync(Guid userId,
        CancellationToken cancellationToken = default)
        => await RevokeAllAsync(userId, SessionRevocationReasons.PasswordReset, "PasswordResetSessionsRevoked",
            cancellationToken);

    private async Task<Result<bool>> RevokeAllAsync(Guid userId, string reason, string auditEvent,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        var count = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await SessionConcurrencyLock.AcquireAsync(context, userId, cancellationToken);
            var revoked = await RevokeAsync(x => x.UserId == userId, reason, cancellationToken);
            if(revoked>0&&audit is not null)await audit.WriteAsync(new AuditEntry(AuditActions.Authentication.Logout,"Authentication","ApplicationUser",userId.ToString(),Outcome:AuditOutcome.Succeeded,Description:"User sessions revoked.",Metadata:new{reason,count=revoked},ActorUserId:userId),cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return revoked;
        });
        logger.LogInformation("{SecurityEvent} for UserId {UserId}, Count {Count}, IP {IpAddress}",
            auditEvent, userId, count, client.IpAddress);
        return Result<bool>.Ok(count > 0);
    }

    public async Task<PagedResult<UserSessionDto>> GetSessionsAsync(Guid userId, PagedRequest request,
        bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var sessions = context.RefreshTokens.AsNoTracking()
            .Where(x => x.UserId == userId)
            .GroupBy(x => x.SessionId)
            .Select(group => new UserSessionDto(
                group.Key,
                group.Min(x => x.CreatedAt),
                group.OrderByDescending(x => x.CreatedAt).Select(x => x.ExpiresAtUtc).First(),
                group.Max(x => x.UsedAtUtc),
                group.OrderByDescending(x => x.CreatedAt).Select(x => x.UserAgent).First(),
                null,
                group.OrderByDescending(x => x.CreatedAt).Select(x => x.CreatedByIp).First(),
                group.OrderByDescending(x => x.CreatedAt)
                    .Select(x => !x.IsUsed && x.RevokedAtUtc == null && x.ExpiresAtUtc > now).First(),
                false));

        if (activeOnly) sessions = sessions.Where(x => x.IsActive);
        var total = await sessions.CountAsync(cancellationToken);
        var items = await sessions.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<UserSessionDto>(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<Result<bool>> RevokeSessionByIdAsync(Guid userId, string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 100)
            return Result<bool>.Fail("Session was not found.", SessionErrorCodes.SessionNotFound);

        var strategy = context.Database.CreateExecutionStrategy();
        var outcome = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await SessionConcurrencyLock.AcquireAsync(context, userId, cancellationToken);
            var owned = await context.RefreshTokens.AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.SessionId == sessionId, cancellationToken);
            if (!owned) return (Owned: false, Count: 0);
            var revoked = await RevokeAsync(x => x.UserId == userId && x.SessionId == sessionId,
                SessionRevocationReasons.UserRevokedSession, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (Owned: true, Count: revoked);
        });
        if (!outcome.Owned)
        {
            logger.LogWarning("SessionRevocationFailed for UserId {UserId}, SessionId {SessionId}, IP {IpAddress}",
                userId, sessionId, client.IpAddress);
            return Result<bool>.Fail("Session was not found.", SessionErrorCodes.SessionNotFound);
        }
        logger.LogInformation("SessionRevoked for UserId {UserId}, SessionId {SessionId}, Count {Count}, IP {IpAddress}",
            userId, sessionId, outcome.Count, client.IpAddress);
        return Result<bool>.Ok(outcome.Count > 0);
    }

    private Task<int> RevokeAsync(System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
        string reason, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var ip = string.IsNullOrWhiteSpace(client.IpAddress) ? null : client.IpAddress.Trim()[..Math.Min(client.IpAddress.Trim().Length, 64)];
        return context.RefreshTokens.Where(predicate).Where(x => x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevokedReason, reason)
                .SetProperty(x => x.RevokedByIp, ip)
                .SetProperty(x => x.Version, x => x.Version + 1), cancellationToken);
    }
}

internal static class SessionConcurrencyLock
{
    public static Task AcquireAsync(SchoolErpDbContext context, Guid userId, CancellationToken cancellationToken)
    {
        var key = BitConverter.ToInt64(userId.ToByteArray(), 0);
        return context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }
}
