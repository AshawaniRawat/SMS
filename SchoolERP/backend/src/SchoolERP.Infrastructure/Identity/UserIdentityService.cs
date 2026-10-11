using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Identity;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;

namespace SchoolERP.Infrastructure.Identity;

public sealed class UserIdentityService(UserManager<ApplicationUser> userManager, SchoolErpDbContext context,
    IValidator<CreatePlatformUserRequest> platformValidator, IValidator<CreateTenantUserRequest> tenantValidator,
    IOptions<IdentitySettings> settings, IEmailVerificationService emailVerificationService,
    TimeProvider timeProvider, ILogger<UserIdentityService> logger) : IUserIdentityService
{
    public async Task<Result<UserSummaryDto>> CreatePlatformUserAsync(CreatePlatformUserRequest request, CancellationToken ct = default)
    {
        var validation = await platformValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Invalid(validation.Errors.Select(x => x.ErrorMessage));
        return await CreateAsync(request.FirstName, request.LastName, request.Email, request.Password,
            request.DisplayName, request.PhoneNumber, null, null, null, ct);
    }

    public async Task<Result<UserSummaryDto>> CreateTenantUserAsync(CreateTenantUserRequest request, CancellationToken ct = default)
    {
        var validation = await tenantValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Invalid(validation.Errors.Select(x => x.ErrorMessage));
        var scopeError = await ValidateScopeAsync(request, ct);
        if (scopeError is not null) return Result<UserSummaryDto>.Fail(scopeError.Value.Message, scopeError.Value.Code);
        return await CreateAsync(request.FirstName, request.LastName, request.Email, request.Password,
            request.DisplayName, request.PhoneNumber, request.TenantId, request.SchoolId, request.CampusId, ct);
    }

    private async Task<Result<UserSummaryDto>> CreateAsync(string firstName, string lastName, string inputEmail, string password,
        string? displayName, string? phoneNumber, Guid? tenantId, Guid? schoolId, Guid? campusId, CancellationToken ct)
    {
        var email = inputEmail.Trim().ToLowerInvariant();
        if (!await IsEmailUniqueAsync(email, ct)) return Result<UserSummaryDto>.Fail("An account with this email address already exists.", UserRegistrationErrorCodes.EmailAlreadyExists);
        var options = settings.Value;
        var user = ApplicationUser.Create(firstName, lastName, email, tenantId, schoolId, campusId,
            options.ActivateNewUsersImmediately, displayName, phoneNumber, options.AutoConfirmAdminCreatedUsers);
        IdentityResult result;
        try { result = await userManager.CreateAsync(user, password); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_users_normalized_email" or "ux_users_normalized_user_name" })
        {
            logger.LogWarning("Identity user creation encountered a duplicate normalized credential for email {Email}", email);
            return Result<UserSummaryDto>.Fail("An account with this email address already exists.", UserRegistrationErrorCodes.EmailAlreadyExists);
        }
        if (!result.Succeeded)
        {
            var error = SafeErrors(result);
            var code = result.Errors.Any(x => x.Code is "DuplicateEmail" or "DuplicateUserName")
                ? UserRegistrationErrorCodes.EmailAlreadyExists : UserRegistrationErrorCodes.IdentityRejected;
            logger.LogWarning("Identity user creation failed for email {Email}: {ErrorCodes}", email, string.Join(",", result.Errors.Select(x => x.Code)));
            return Result<UserSummaryDto>.Fail(error, code);
        }
        logger.LogInformation("Identity user {UserId} created with TenantId {TenantId}, SchoolId {SchoolId}, CampusId {CampusId}",
            user.Id, user.TenantId, user.SchoolId, user.CampusId);
        if (!user.EmailConfirmed)
            await emailVerificationService.SendVerificationAsync(user.Id, cancellationToken: ct);
        return Result<UserSummaryDto>.Ok(Map(user));
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default) =>
        await userManager.FindByEmailAsync(email.Trim()) is null;

    public async Task<UserSummaryDto?> GetTenantUserByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) =>
        await userManager.Users.AsNoTracking().Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(Project()).SingleOrDefaultAsync(ct);

    public async Task<UserSummaryDto?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        return user is null ? null : Map(user);
    }

    public async Task<PagedResult<UserSummaryDto>> GetUsersForTenantAsync(Guid tenantId, PagedRequest page, CancellationToken ct = default)
    {
        var query = userManager.Users.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize).Select(Project()).ToListAsync(ct);
        return new PagedResult<UserSummaryDto>(items, page.PageNumber, page.PageSize, total);
    }

    public Task<Result<bool>> ActivateTenantUserAsync(Guid tenantId, Guid id, CancellationToken ct = default) => ChangeState(tenantId, id, "activated", x => x.Activate(), false, ct);
    public Task<Result<bool>> SuspendTenantUserAsync(Guid tenantId, Guid id, CancellationToken ct = default) => ChangeState(tenantId, id, "suspended", x => x.Suspend(), true, ct);
    public Task<Result<bool>> DeactivateTenantUserAsync(Guid tenantId, Guid id, CancellationToken ct = default) => ChangeState(tenantId, id, "deactivated", x => x.Deactivate(Now()), true, ct);

    private async Task<Result<bool>> ChangeState(Guid tenantId, Guid id, string action, Action<ApplicationUser> transition, bool lockout, CancellationToken ct)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (user is null) return Result<bool>.Fail("User was not found.");
        try { transition(user); }
        catch (Exception ex) { return Result<bool>.Fail(ex.Message); }
        user.LockoutEnd = lockout ? DateTimeOffset.MaxValue : null;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return Result<bool>.Fail(SafeErrors(result));
        logger.LogInformation("Identity user {UserId} {Action}", user.Id, action);
        return Result<bool>.Ok(true);
    }

    private async Task<(string Message, string Code)?> ValidateScopeAsync(CreateTenantUserRequest r, CancellationToken ct)
    {
        var tenant = await context.Tenants.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.TenantId, ct);
        if (tenant is null) return ("The selected tenant was not found.", UserRegistrationErrorCodes.ScopeNotFound);
        if (tenant.Status is TenantStatus.Suspended or TenantStatus.Archived) return ("New users cannot be created for this tenant.", UserRegistrationErrorCodes.ScopeNotOperational);
        if (r.SchoolId.HasValue)
        {
            var school = await context.Schools.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.SchoolId, ct);
            if (school is null || school.TenantId != r.TenantId) return ("The selected school was not found.", UserRegistrationErrorCodes.ScopeNotFound);
            if (school.Status is SchoolStatus.Suspended or SchoolStatus.Archived) return ("New users cannot be assigned to this school.", UserRegistrationErrorCodes.ScopeNotOperational);
        }
        if (r.CampusId.HasValue)
        {
            var campus = await context.Campuses.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.CampusId, ct);
            if (campus is null || campus.TenantId != r.TenantId || campus.SchoolId != r.SchoolId) return ("The selected campus was not found.", UserRegistrationErrorCodes.ScopeNotFound);
            if (campus.Status is CampusStatus.Suspended or CampusStatus.Archived) return ("New users cannot be assigned to this campus.", UserRegistrationErrorCodes.ScopeNotOperational);
        }
        return null;
    }

    private static Result<UserSummaryDto> Invalid(IEnumerable<string> errors) =>
        Result<UserSummaryDto>.Fail(string.Join(" ", errors.Distinct()), UserRegistrationErrorCodes.Validation);

    private static string SafeErrors(IdentityResult result) => string.Join(" ", result.Errors.Select(x => x.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "An account with this email address already exists.",
        "PasswordTooShort" => "The password is too short.",
        "PasswordRequiresUpper" => "The password requires an uppercase letter.",
        "PasswordRequiresLower" => "The password requires a lowercase letter.",
        "PasswordRequiresDigit" => "The password requires a number.",
        "PasswordRequiresNonAlphanumeric" => "The password requires a special character.",
        _ => x.Description
    }).Distinct());
    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private static UserSummaryDto Map(ApplicationUser x) => new(x.Id, x.FirstName, x.LastName, x.Email!, x.DisplayName, x.PhoneNumber, x.TenantId, x.SchoolId, x.CampusId, x.Status, x.IsActive, x.EmailConfirmed, x.CreatedAt);
    private static System.Linq.Expressions.Expression<Func<ApplicationUser, UserSummaryDto>> Project() => x => new(x.Id, x.FirstName, x.LastName, x.Email!, x.DisplayName, x.PhoneNumber, x.TenantId, x.SchoolId, x.CampusId, x.Status, x.IsActive, x.EmailConfirmed, x.CreatedAt);
}
