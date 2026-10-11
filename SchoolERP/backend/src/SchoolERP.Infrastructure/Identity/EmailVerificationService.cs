using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Security;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;

namespace SchoolERP.Infrastructure.Identity;

public sealed class EmailVerificationService(
    UserManager<ApplicationUser> userManager,
    SchoolErpDbContext context,
    IValidator<ConfirmEmailRequest> confirmValidator,
    IIdentityTokenEncoder tokenEncoder,
    IEmailService emailService,
    IOptions<FrontendOptions> frontend,
    IOptions<IdentitySettings> identitySettings,
    ILogger<EmailVerificationService> logger) : IEmailVerificationService
{
    public async Task<Result<bool>> SendVerificationAsync(Guid userId, string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.EmailConfirmed || user.Status == UserAccountStatus.Deactivated)
            return Result<bool>.Ok(false);

        logger.LogInformation("EmailVerificationRequested for UserId {UserId}, IP {IpAddress}", user.Id, ipAddress);
        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = tokenEncoder.Encode(rawToken);
        var link = $"{frontend.Value.BaseUrl.TrimEnd('/')}/verify-email?userId={user.Id:D}&token={Uri.EscapeDataString(encoded)}";
        try
        {
            await emailService.SendEmailVerificationAsync(user.Email!, link, cancellationToken);
            logger.LogInformation("EmailVerificationSent for UserId {UserId}, IP {IpAddress}", user.Id, ipAddress);
            return Result<bool>.Ok(true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "EmailVerificationDeliveryFailed for UserId {UserId}, IP {IpAddress}",
                user.Id, ipAddress);
            return Result<bool>.Fail("Verification email delivery failed.");
        }
    }

    public async Task RequestResendAsync(string email, string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        logger.LogInformation("EmailVerificationResendRequested for UserId {UserId}, IP {IpAddress}",
            user?.Id, ipAddress);
        if (user is null || user.EmailConfirmed || user.Status == UserAccountStatus.Deactivated) return;
        await SendVerificationAsync(user.Id, ipAddress, cancellationToken);
    }

    public async Task<Result<bool>> ConfirmEmailAsync(ConfirmEmailRequest request, string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = await confirmValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<bool>.Fail(EmailVerificationMessages.InvalidOrExpired,
                EmailVerificationErrorCodes.InvalidRequest);

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return Invalid(request.UserId, ipAddress);
        if (user.EmailConfirmed) return Result<bool>.Ok(true);
        if (!tokenEncoder.TryDecode(request.Token, out var rawToken)) return Invalid(user.Id, ipAddress);

        var confirmation = await userManager.ConfirmEmailAsync(user, rawToken);
        if (!confirmation.Succeeded) return Invalid(user.Id, ipAddress);

        if (identitySettings.Value.ActivateUserAfterEmailConfirmation &&
            user.Status == UserAccountStatus.Pending && user.TenantId.HasValue &&
            await IsScopeOperationalAsync(user, cancellationToken))
        {
            user.Activate();
            var activation = await userManager.UpdateAsync(user);
            if (!activation.Succeeded)
                logger.LogError("EmailVerifiedAutoActivationFailed for UserId {UserId}, Codes {Codes}", user.Id,
                    string.Join(",", activation.Errors.Select(x => x.Code)));
        }

        logger.LogInformation("EmailVerificationSucceeded EmailVerified for UserId {UserId}, IP {IpAddress}",
            user.Id, ipAddress);
        return Result<bool>.Ok(true);
    }

    private async Task<bool> IsScopeOperationalAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!user.TenantId.HasValue) return false; // Platform accounts are never auto-activated by email ownership.
        if (!await context.Tenants.AsNoTracking().AnyAsync(
                x => x.Id == user.TenantId && x.Status == TenantStatus.Active, cancellationToken)) return false;
        if (user.SchoolId.HasValue && !await context.Schools.AsNoTracking().IgnoreQueryFilters().AnyAsync(
                x => x.Id == user.SchoolId && x.TenantId == user.TenantId && x.Status == SchoolStatus.Active,
                cancellationToken)) return false;
        return !user.CampusId.HasValue || await context.Campuses.AsNoTracking().IgnoreQueryFilters().AnyAsync(
            x => x.Id == user.CampusId && x.TenantId == user.TenantId && x.SchoolId == user.SchoolId &&
                 x.Status == CampusStatus.Active, cancellationToken);
    }

    private Result<bool> Invalid(Guid userId, string? ipAddress)
    {
        logger.LogWarning("EmailVerificationFailed for UserId {UserId}, IP {IpAddress}", userId, ipAddress);
        return Result<bool>.Fail(EmailVerificationMessages.InvalidOrExpired,
            EmailVerificationErrorCodes.InvalidOrExpired);
    }
}
