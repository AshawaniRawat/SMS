using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.Authentication;

public sealed record ConfirmEmailRequest(Guid UserId, string Token);
public sealed record ResendEmailVerificationRequest(string Email);

public interface IEmailVerificationService
{
    Task<Result<bool>> SendVerificationAsync(Guid userId, string? ipAddress = null,
        CancellationToken cancellationToken = default);
    Task<Result<bool>> ConfirmEmailAsync(ConfirmEmailRequest request, string? ipAddress = null,
        CancellationToken cancellationToken = default);
    Task RequestResendAsync(string email, string? ipAddress = null,
        CancellationToken cancellationToken = default);
}

public static class EmailVerificationMessages
{
    public const string Confirmed = "Email verified successfully.";
    public const string InvalidOrExpired = "Invalid or expired email verification request.";
    public const string ResendAccepted = "If an account exists and requires verification, a verification email has been sent.";
}

public static class EmailVerificationErrorCodes
{
    public const string InvalidRequest = "Auth.InvalidEmailVerificationRequest";
    public const string InvalidOrExpired = "Auth.InvalidOrExpiredEmailVerification";
}
