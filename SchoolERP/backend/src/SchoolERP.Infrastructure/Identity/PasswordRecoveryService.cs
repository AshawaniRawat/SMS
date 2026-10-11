using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Security;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Results;

namespace SchoolERP.Infrastructure.Identity;

public sealed class PasswordRecoveryService(
    UserManager<ApplicationUser> userManager,
    IValidator<ResetPasswordRequest> resetValidator,
    IIdentityTokenEncoder tokenEncoder,
    IEmailService emailService,
    ISessionService sessionService,
    IOptions<FrontendOptions> frontend,
    TimeProvider timeProvider,
    ILogger<PasswordRecoveryService> logger) : IPasswordRecoveryService
{
    public async Task RequestPasswordResetAsync(string email, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        logger.LogInformation("PasswordResetRequested for UserId {UserId}, IP {IpAddress}", user?.Id, ipAddress);
        if (user is null || user.Status == UserAccountStatus.Deactivated) return;

        var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = tokenEncoder.Encode(rawToken);
        var baseUri = frontend.Value.BaseUrl.TrimEnd('/');
        var link = $"{baseUri}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(encodedToken)}";
        try
        {
            await emailService.SendPasswordResetEmailAsync(user.Email!, link, cancellationToken);
            logger.LogInformation("PasswordResetEmailSent for UserId {UserId}, IP {IpAddress}", user.Id, ipAddress);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PasswordResetEmailDeliveryFailed for UserId {UserId}, IP {IpAddress}",
                user.Id, ipAddress);
        }
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validation = await resetValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<bool>.Fail(string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)),
                PasswordRecoveryErrorCodes.InvalidRequest);

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.Status == UserAccountStatus.Deactivated ||
            !tokenEncoder.TryDecode(request.Token, out var rawToken))
            return Invalid(user?.Id, ipAddress);

        var reset = await userManager.ResetPasswordAsync(user, rawToken, request.NewPassword);
        if (!reset.Succeeded)
        {
            if (reset.Errors.Any(x => x.Code.Contains("InvalidToken", StringComparison.OrdinalIgnoreCase)))
                return Invalid(user.Id, ipAddress);
            logger.LogWarning("PasswordResetFailed for UserId {UserId}, IP {IpAddress}, Codes {Codes}", user.Id,
                ipAddress, string.Join(",", reset.Errors.Select(x => x.Code)));
            return Result<bool>.Fail(string.Join(" ", reset.Errors.Select(x => x.Description)),
                PasswordRecoveryErrorCodes.PasswordPolicy);
        }

        user.PasswordChangedAt = timeProvider.GetUtcNow().UtcDateTime;
        var stamp = await userManager.UpdateSecurityStampAsync(user);
        var failures = await userManager.ResetAccessFailedCountAsync(user);
        var unlock = await userManager.SetLockoutEndDateAsync(user, null);
        if (!stamp.Succeeded || !failures.Succeeded || !unlock.Succeeded)
        {
            logger.LogCritical("PasswordResetSecurityStateUpdateFailed for UserId {UserId}, IP {IpAddress}",
                user.Id, ipAddress);
            return Result<bool>.Fail("Password reset could not be completed securely.",
                PasswordRecoveryErrorCodes.SessionRevocationFailed);
        }

        try
        {
            await sessionService.RevokeAllAfterPasswordResetAsync(user.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "PasswordResetSessionRevocationFailed for UserId {UserId}, IP {IpAddress}",
                user.Id, ipAddress);
            return Result<bool>.Fail("Password reset could not be completed securely.",
                PasswordRecoveryErrorCodes.SessionRevocationFailed);
        }

        logger.LogInformation("PasswordResetSucceeded for UserId {UserId}, IP {IpAddress}", user.Id, ipAddress);
        return Result<bool>.Ok(true);
    }

    private Result<bool> Invalid(Guid? userId, string? ipAddress)
    {
        logger.LogWarning("PasswordResetInvalidToken for UserId {UserId}, IP {IpAddress}", userId, ipAddress);
        return Result<bool>.Fail(PasswordRecoveryMessages.InvalidReset, PasswordRecoveryErrorCodes.InvalidOrExpired);
    }
}
