using SchoolERP.Application.Common.Pagination;
using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.Authentication;

public sealed record LogoutRequest(string? RefreshToken);

public sealed record UserSessionDto(
    string SessionId,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? LastUsedAtUtc,
    string? UserAgent,
    string? DeviceName,
    string? CreatedByIp,
    bool IsActive,
    bool IsCurrent);

public interface ISessionService
{
    Task<Result<bool>> LogoutSessionAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeAllAfterPasswordResetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<UserSessionDto>> GetSessionsAsync(Guid userId, PagedRequest request, bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeSessionByIdAsync(Guid userId, string sessionId,
        CancellationToken cancellationToken = default);
}

public static class SessionErrorCodes
{
    public const string InvalidRequest = "Auth.InvalidLogoutRequest";
    public const string SessionNotFound = "Auth.SessionNotFound";
    public const string SessionRevocationFailed = "Auth.SessionRevocationFailed";
}

public static class SessionRevocationReasons
{
    public const string Logout = "Logout";
    public const string LogoutAllDevices = "LogoutAllDevices";
    public const string UserRevokedSession = "UserRevokedSession";
    public const string PasswordReset = "PasswordReset";
}
