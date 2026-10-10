using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Identity;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;

namespace SchoolERP.Infrastructure.Identity;

public sealed class UserIdentityService(UserManager<ApplicationUser> userManager, SchoolErpDbContext context,
    IValidator<CreateUserRequest> validator, TimeProvider timeProvider, ILogger<UserIdentityService> logger) : IUserIdentityService
{
    public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result<Guid>.Fail(string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct()));
        var scopeError = await ValidateScopeAsync(request, ct);
        if (scopeError is not null) return Result<Guid>.Fail(scopeError);
        var email = request.Email.Trim().ToLowerInvariant();
        if (!await IsEmailUniqueAsync(email, ct)) return Result<Guid>.Fail("An account with this email address already exists.");
        var user = ApplicationUser.Create(request.FirstName, request.LastName, email, request.TenantId, request.SchoolId, request.CampusId, request.ActivateImmediately);
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var error = SafeErrors(result);
            logger.LogWarning("Identity user creation failed for email {Email}: {ErrorCodes}", email, string.Join(",", result.Errors.Select(x => x.Code)));
            return Result<Guid>.Fail(error);
        }
        logger.LogInformation("Identity user {UserId} created", user.Id);
        return Result<Guid>.Ok(user.Id);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default) =>
        await userManager.FindByEmailAsync(email.Trim()) is null;

    public async Task<UserSummaryDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default) =>
        await userManager.Users.AsNoTracking().Where(x => x.Id == id).Select(Project()).SingleOrDefaultAsync(ct);

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

    public Task<Result<bool>> ActivateUserAsync(Guid id, CancellationToken ct = default) => ChangeState(id, "activated", x => x.Activate(), false, ct);
    public Task<Result<bool>> SuspendUserAsync(Guid id, CancellationToken ct = default) => ChangeState(id, "suspended", x => x.Suspend(), true, ct);
    public Task<Result<bool>> DeactivateUserAsync(Guid id, CancellationToken ct = default) => ChangeState(id, "deactivated", x => x.Deactivate(Now()), true, ct);

    private async Task<Result<bool>> ChangeState(Guid id, string action, Action<ApplicationUser> transition, bool lockout, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result<bool>.Fail("User was not found.");
        try { transition(user); }
        catch (Exception ex) { return Result<bool>.Fail(ex.Message); }
        user.LockoutEnd = lockout ? DateTimeOffset.MaxValue : null;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return Result<bool>.Fail(SafeErrors(result));
        logger.LogInformation("Identity user {UserId} {Action}", user.Id, action);
        return Result<bool>.Ok(true);
    }

    private async Task<string?> ValidateScopeAsync(CreateUserRequest r, CancellationToken ct)
    {
        if (!r.TenantId.HasValue) return r.SchoolId.HasValue || r.CampusId.HasValue ? "Platform users cannot be assigned to a school or campus." : null;
        var tenant = await context.Tenants.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.TenantId, ct);
        if (tenant is null) return "The selected tenant does not exist.";
        if (tenant.Status is TenantStatus.Suspended or TenantStatus.Archived) return "New users cannot be created for a suspended or archived tenant.";
        if (r.SchoolId.HasValue)
        {
            var school = await context.Schools.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.SchoolId, ct);
            if (school is null || school.TenantId != r.TenantId) return "The selected school does not belong to the tenant.";
            if (school.Status is SchoolStatus.Suspended or SchoolStatus.Archived) return "New users cannot be assigned to a suspended or archived school.";
        }
        if (r.CampusId.HasValue)
        {
            var campus = await context.Campuses.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == r.CampusId, ct);
            if (campus is null || campus.TenantId != r.TenantId || campus.SchoolId != r.SchoolId) return "The selected campus does not belong to the tenant and school.";
            if (campus.Status == CampusStatus.Archived) return "New users cannot be assigned to an archived campus.";
        }
        return null;
    }

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
    private static UserSummaryDto Map(ApplicationUser x) => new(x.Id, x.FirstName, x.LastName, x.Email!, x.DisplayName, x.TenantId, x.SchoolId, x.CampusId, x.Status, x.IsActive, x.EmailConfirmed, x.TwoFactorEnabled, x.CreatedAt);
    private static System.Linq.Expressions.Expression<Func<ApplicationUser, UserSummaryDto>> Project() => x => new(x.Id, x.FirstName, x.LastName, x.Email!, x.DisplayName, x.TenantId, x.SchoolId, x.CampusId, x.Status, x.IsActive, x.EmailConfirmed, x.TwoFactorEnabled, x.CreatedAt);
}
