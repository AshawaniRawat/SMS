using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;
using SchoolERP.Application.Security;

namespace SchoolERP.Infrastructure.Identity;

public sealed class AuthenticationService(UserManager<ApplicationUser> userManager, SchoolErpDbContext context,
    IValidator<LoginRequest> validator, IOptions<IdentitySettings> settings, TimeProvider timeProvider,
    IAccessTokenService accessTokenService, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public async Task<Result<LoginResponse>> AuthenticateAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Fail("The login request is invalid.", AuthenticationErrorCodes.InvalidRequest);

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            logger.LogWarning("LoginFailed for unknown account");
            return InvalidCredentials();
        }

        if (userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("LoginLockedOut for UserId {UserId}, TenantId {TenantId}", user.Id, user.TenantId);
            return Fail("Your account is temporarily locked. Please try again later.", AuthenticationErrorCodes.AccountLocked);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            if (userManager.SupportsUserLockout && user.LockoutEnabled)
            {
                var accessFailure = await userManager.AccessFailedAsync(user);
                if (!accessFailure.Succeeded)
                {
                    logger.LogError("Identity failed to record a login failure for UserId {UserId}: {Codes}", user.Id,
                        string.Join(",", accessFailure.Errors.Select(x => x.Code)));
                    return Fail("Authentication could not be completed.", AuthenticationErrorCodes.PersistenceFailure);
                }
            }
            logger.LogWarning("LoginFailed for UserId {UserId}, TenantId {TenantId}", user.Id, user.TenantId);
            return InvalidCredentials();
        }

        if (userManager.SupportsUserLockout && user.AccessFailedCount > 0)
        {
            var reset = await userManager.ResetAccessFailedCountAsync(user);
            if (!reset.Succeeded) return Fail("Authentication could not be completed.", AuthenticationErrorCodes.PersistenceFailure);
        }

        var accountFailure = ValidateAccount(user);
        if (accountFailure is not null)
        {
            logger.LogWarning("LoginRejectedAccountStatus for UserId {UserId}, Status {Status}", user.Id, user.Status);
            return accountFailure;
        }

        if (settings.Value.RequireConfirmedEmail && !user.EmailConfirmed)
        {
            logger.LogWarning("LoginRejectedEmailConfirmation for UserId {UserId}", user.Id);
            return Fail("Email confirmation is required before signing in.", AuthenticationErrorCodes.EmailConfirmationRequired);
        }

        var organizationFailure = await ValidateOrganizationAsync(user, ct);
        if (organizationFailure is not null) return organizationFailure;

        if (user.TwoFactorEnabled)
        {
            logger.LogInformation("LoginRequiresTwoFactor for UserId {UserId}, TenantId {TenantId}", user.Id, user.TenantId);
            return Result<LoginResponse>.Ok(new(true, null, null, null, Map(user)));
        }

        user.RecordSuccessfulLogin(timeProvider.GetUtcNow().UtcDateTime);
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            logger.LogError("Identity failed to record successful login for UserId {UserId}: {Codes}", user.Id,
                string.Join(",", update.Errors.Select(x => x.Code)));
            return Fail("Authentication could not be completed.", AuthenticationErrorCodes.PersistenceFailure);
        }
        logger.LogInformation("LoginSucceeded for UserId {UserId}, TenantId {TenantId}", user.Id, user.TenantId);
        var authenticatedUser = Map(user);
        var token = accessTokenService.GenerateToken(authenticatedUser);
        return Result<LoginResponse>.Ok(new(false, token.AccessToken, token.ExpiresAtUtc, token.TokenType, authenticatedUser));
    }

    private static Result<LoginResponse>? ValidateAccount(ApplicationUser user)
    {
        if (user.Status == UserAccountStatus.Active && user.IsActive) return null;
        return user.Status switch
        {
            UserAccountStatus.Pending => Fail("Your account is pending activation.", AuthenticationErrorCodes.AccountPending),
            UserAccountStatus.Suspended => Fail("Your account is suspended.", AuthenticationErrorCodes.AccountSuspended),
            UserAccountStatus.Locked => Fail("Your account is unavailable.", AuthenticationErrorCodes.AccountLocked),
            UserAccountStatus.Deactivated => Fail("Your account is deactivated.", AuthenticationErrorCodes.AccountDeactivated),
            _ => Fail("Your account is unavailable.", AuthenticationErrorCodes.AccountUnavailable)
        };
    }

    private async Task<Result<LoginResponse>?> ValidateOrganizationAsync(ApplicationUser user, CancellationToken ct)
    {
        if (!user.TenantId.HasValue)
        {
            if (!user.SchoolId.HasValue && !user.CampusId.HasValue) return null;
            return InvalidScope(user);
        }
        if (user.CampusId.HasValue && !user.SchoolId.HasValue) return InvalidScope(user);

        var tenantStatus = await context.Tenants.AsNoTracking().Where(x => x.Id == user.TenantId)
            .Select(x => (TenantStatus?)x.Status).SingleOrDefaultAsync(ct);
        if (tenantStatus != TenantStatus.Active)
        {
            logger.LogWarning("LoginRejectedTenantStatus for UserId {UserId}, TenantId {TenantId}", user.Id, user.TenantId);
            return Fail("Your organization is currently unavailable.", AuthenticationErrorCodes.TenantUnavailable);
        }
        if (user.SchoolId.HasValue)
        {
            var school = await context.Schools.AsNoTracking().IgnoreQueryFilters().Where(x => x.Id == user.SchoolId)
                .Select(x => new { x.TenantId, x.Status }).SingleOrDefaultAsync(ct);
            if (school is null || school.TenantId != user.TenantId) return InvalidScope(user);
            if (school.Status != SchoolStatus.Active)
            {
                logger.LogWarning("LoginRejectedSchoolStatus for UserId {UserId}, SchoolId {SchoolId}", user.Id, user.SchoolId);
                return Fail("Your organization is currently unavailable.", AuthenticationErrorCodes.SchoolUnavailable);
            }
        }
        if (user.CampusId.HasValue)
        {
            var campus = await context.Campuses.AsNoTracking().IgnoreQueryFilters().Where(x => x.Id == user.CampusId)
                .Select(x => new { x.TenantId, x.SchoolId, x.Status }).SingleOrDefaultAsync(ct);
            if (campus is null || campus.TenantId != user.TenantId || campus.SchoolId != user.SchoolId) return InvalidScope(user);
            if (campus.Status != CampusStatus.Active)
            {
                logger.LogWarning("LoginRejectedCampusStatus for UserId {UserId}, CampusId {CampusId}", user.Id, user.CampusId);
                return Fail("Your organization is currently unavailable.", AuthenticationErrorCodes.CampusUnavailable);
            }
        }
        return null;
    }

    private Result<LoginResponse> InvalidScope(ApplicationUser user)
    {
        logger.LogWarning("LoginRejectedInvalidScope for UserId {UserId}, TenantId {TenantId}, SchoolId {SchoolId}, CampusId {CampusId}",
            user.Id, user.TenantId, user.SchoolId, user.CampusId);
        return Fail("Your account is unavailable.", AuthenticationErrorCodes.InvalidUserScope);
    }

    private static AuthenticatedUserDto Map(ApplicationUser x) => new(x.Id, x.FirstName, x.LastName, x.Email!,
        x.DisplayName, x.TenantId, x.SchoolId, x.CampusId, x.Status, x.EmailConfirmed, x.TwoFactorEnabled, x.LastLoginAt);
    private static Result<LoginResponse> InvalidCredentials() => Fail("Invalid email or password.", AuthenticationErrorCodes.InvalidCredentials);
    private static Result<LoginResponse> Fail(string message, string code) => Result<LoginResponse>.Fail(message, code);
}
