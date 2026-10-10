using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Authentication;

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthenticatedUserDto(Guid UserId, string FirstName, string LastName, string Email,
    string? DisplayName, Guid? TenantId, Guid? SchoolId, Guid? CampusId, UserAccountStatus Status,
    bool EmailConfirmed, bool TwoFactorEnabled, DateTime? LastLoginAt);

public sealed record LoginResponse(bool RequiresTwoFactor, string? AccessToken, DateTime? AccessTokenExpiresAtUtc,
    string? TokenType, AuthenticatedUserDto User);

public static class AuthenticationErrorCodes
{
    public const string InvalidRequest = "Auth.InvalidRequest";
    public const string InvalidCredentials = "Auth.InvalidCredentials";
    public const string AccountLocked = "Auth.AccountLocked";
    public const string AccountPending = "Auth.AccountPending";
    public const string AccountSuspended = "Auth.AccountSuspended";
    public const string AccountDeactivated = "Auth.AccountDeactivated";
    public const string AccountUnavailable = "Auth.AccountUnavailable";
    public const string EmailConfirmationRequired = "Auth.EmailConfirmationRequired";
    public const string TenantUnavailable = "Auth.TenantUnavailable";
    public const string SchoolUnavailable = "Auth.SchoolUnavailable";
    public const string CampusUnavailable = "Auth.CampusUnavailable";
    public const string InvalidUserScope = "Auth.InvalidUserScope";
    public const string PersistenceFailure = "Auth.PersistenceFailure";
}
