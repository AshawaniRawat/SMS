using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.Authentication;

public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword, string ConfirmPassword);

public interface IPasswordRecoveryService
{
    Task RequestPasswordResetAsync(string email, string? ipAddress, CancellationToken cancellationToken = default);
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress,
        CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}

public interface IIdentityTokenEncoder
{
    string Encode(string token);
    bool TryDecode(string encodedToken, out string token);
}

public static class PasswordRecoveryMessages
{
    public const string ForgotPasswordAccepted = "If an account exists for this email, password reset instructions have been sent.";
    public const string ResetSucceeded = "Your password has been reset successfully.";
    public const string InvalidReset = "Invalid or expired password reset request.";
}

public static class PasswordRecoveryErrorCodes
{
    public const string InvalidRequest = "Auth.InvalidPasswordResetRequest";
    public const string InvalidOrExpired = "Auth.InvalidOrExpiredPasswordReset";
    public const string PasswordPolicy = "Auth.PasswordPolicyFailed";
    public const string SessionRevocationFailed = "Auth.PasswordResetSessionRevocationFailed";
}
