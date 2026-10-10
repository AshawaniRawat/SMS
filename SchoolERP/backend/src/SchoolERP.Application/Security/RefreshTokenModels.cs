using SchoolERP.Application.Features.Authentication;
using SchoolERP.Shared.Results;
namespace SchoolERP.Application.Security;
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RefreshTokenIssueResult(string RefreshToken, DateTime RefreshTokenExpiresAtUtc, string SessionId);
public sealed record TokenPairResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc, string TokenType, AuthenticatedUserDto User);
public interface IRefreshTokenService
{
    Task<RefreshTokenIssueResult> CreateAsync(AuthenticatedUserDto user, string securityStamp, CancellationToken cancellationToken = default);
    Task<Result<TokenPairResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeAsync(string refreshToken, string reason, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeSessionAsync(Guid userId, string sessionId, string reason, CancellationToken cancellationToken = default);
}
public static class RefreshTokenErrorCodes
{
    public const string InvalidRequest = "Auth.InvalidRefreshRequest";
    public const string Invalid = "Auth.InvalidRefreshToken";
    public const string Expired = "Auth.RefreshTokenExpired";
    public const string Revoked = "Auth.RefreshTokenRevoked";
    public const string AlreadyUsed = "Auth.RefreshTokenAlreadyUsed";
    public const string ReuseDetected = "Auth.RefreshTokenReuseDetected";
    public const string SessionRevoked = "Auth.SessionRevoked";
    public const string AccountUnavailable = "Auth.AccountUnavailable";
    public const string PersistenceFailure = "Auth.RefreshPersistenceFailure";
}
