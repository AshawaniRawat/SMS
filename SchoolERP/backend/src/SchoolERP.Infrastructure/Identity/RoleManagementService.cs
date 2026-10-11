using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Roles;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Shared.Results;

namespace SchoolERP.Infrastructure.Identity;

public sealed class RoleManagementService(
    SchoolErpDbContext context,
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IValidator<CreateTenantRoleRequest> createValidator,
    IValidator<UpdateRoleRequest> updateValidator) : IRoleManagementService
{
    public async Task<Result<RoleDto>> CreateTenantRoleAsync(Guid tenantId, CreateTenantRoleRequest request,
        CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Fail(string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)), RoleErrorCodes.Validation);
        var tenantStatus = await context.Tenants.AsNoTracking().Where(x => x.Id == tenantId)
            .Select(x => (TenantStatus?)x.Status).SingleOrDefaultAsync(ct);
        if (tenantStatus is null) return Fail("Tenant was not found.", RoleErrorCodes.NotFound);
        if (tenantStatus is TenantStatus.Suspended or TenantStatus.Archived)
            return Fail("Roles cannot be created for this tenant.", RoleErrorCodes.ScopeMismatch);
        var code = ApplicationRole.NormalizeCode(request.Code);
        if (await context.Roles.AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct)) return Duplicate();
        var role = ApplicationRole.CreateTenant(tenantId, code, request.DisplayName, request.Type,
            request.Description, request.SortOrder);
        try
        {
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded) return Fail(SafeErrors(result), RoleErrorCodes.PersistenceFailure);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Duplicate(); }
        return Result<RoleDto>.Ok(await MapAsync(role, tenantId, ct));
    }

    public async Task<PagedResult<RoleDto>> ListTenantRolesAsync(Guid tenantId, PagedRequest page,
        RoleStatus? status = null, RoleType? type = null, bool? isSystemRole = null, string? search = null,
        CancellationToken ct = default)
    {
        var query = context.Roles.AsNoTracking().Where(x => x.TenantId == tenantId ||
            (x.TenantId == null && x.Type != RoleType.Platform));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (type.HasValue) query = query.Where(x => x.Type == type);
        if (isSystemRole.HasValue) query = query.Where(x => x.IsSystemRole == isSystemRole);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim(); query = query.Where(x => x.Code.Contains(term) || x.DisplayName.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName)
            .Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize).ToListAsync(ct);
        var result = new List<RoleDto>(rows.Count);
        foreach (var role in rows) result.Add(await MapAsync(role, tenantId, ct));
        return new(result, page.PageNumber, page.PageSize, total);
    }

    public async Task<RoleDto?> GetTenantVisibleRoleAsync(Guid tenantId, Guid roleId, CancellationToken ct = default)
    {
        var role = await context.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == roleId &&
            (x.TenantId == tenantId || (x.TenantId == null && x.Type != RoleType.Platform)), ct);
        return role is null ? null : await MapAsync(role, tenantId, ct);
    }

    public async Task<Result<RoleDto>> UpdateTenantRoleAsync(Guid tenantId, Guid roleId, UpdateRoleRequest request,
        CancellationToken ct = default)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Fail(string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)), RoleErrorCodes.Validation);
        return await Mutate(tenantId, roleId, x => x.Update(request.DisplayName, request.Description, request.SortOrder), ct);
    }

    public Task<Result<RoleDto>> ActivateAsync(Guid tenantId, Guid roleId, CancellationToken ct = default) =>
        Mutate(tenantId, roleId, x => x.Activate(), ct);
    public Task<Result<RoleDto>> DeactivateAsync(Guid tenantId, Guid roleId, CancellationToken ct = default) =>
        Mutate(tenantId, roleId, x => x.Deactivate(), ct);

    public async Task<Result<RoleDto>> ArchiveAsync(Guid tenantId, Guid roleId, CancellationToken ct = default)
    {
        var role = await TenantOperationRole(tenantId, roleId, ct);
        if (role is null) return NotFound();
        if (role.IsSystemRole) return Protected();
        if (await context.Set<IdentityUserRole<Guid>>().AnyAsync(x => x.RoleId == roleId, ct))
            return Fail("Users must be unassigned before archiving this role.", RoleErrorCodes.HasAssignedUsers);
        return await MutateLoaded(tenantId, role, x => x.Archive(), ct);
    }

    public async Task<Result<bool>> AssignRoleAsync(Guid tenantId, Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var pair = await LoadAssignment(tenantId, userId, roleId, ct);
        if (pair.Error is not null) return Result<bool>.Fail(pair.Error.Value.Message, pair.Error.Value.Code);
        var user = pair.User!; var role = pair.Role!;
        if (!role.IsActive || role.Status != RoleStatus.Active)
            return Result<bool>.Fail("Inactive roles cannot be assigned.", RoleErrorCodes.Inactive);
        if (!ScopeMatches(user, role)) return Result<bool>.Fail("Role scope does not match the user.", RoleErrorCodes.ScopeMismatch);
        if (await userManager.IsInRoleAsync(user, role.Name!)) return Result<bool>.Ok(true);
        var result = await userManager.AddToRoleAsync(user, role.Name!);
        return result.Succeeded ? Result<bool>.Ok(true) : Result<bool>.Fail(SafeErrors(result), RoleErrorCodes.PersistenceFailure);
    }

    public async Task<Result<bool>> RemoveRoleAsync(Guid tenantId, Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var pair = await LoadAssignment(tenantId, userId, roleId, ct);
        if (pair.Error is not null) return Result<bool>.Fail(pair.Error.Value.Message, pair.Error.Value.Code);
        var user = pair.User!; var role = pair.Role!;
        if (!ScopeMatches(user, role)) return Result<bool>.Fail("Role scope does not match the user.", RoleErrorCodes.ScopeMismatch);
        if (!await userManager.IsInRoleAsync(user, role.Name!)) return Result<bool>.Ok(true);
        var result = await userManager.RemoveFromRoleAsync(user, role.Name!);
        return result.Succeeded ? Result<bool>.Ok(true) : Result<bool>.Fail(SafeErrors(result), RoleErrorCodes.PersistenceFailure);
    }

    public async Task<IReadOnlyCollection<RoleDto>?> GetUserRolesAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId, ct);
        if (user is null) return null;
        var roles = await (from role in context.Roles.AsNoTracking()
            join assignment in context.Set<IdentityUserRole<Guid>>() on role.Id equals assignment.RoleId
            where assignment.UserId == userId select role).ToListAsync(ct);
        var result = new List<RoleDto>();
        foreach (var role in roles) result.Add(await MapAsync(role, tenantId, ct));
        return result;
    }

    private async Task<Result<RoleDto>> Mutate(Guid tenantId, Guid roleId, Action<ApplicationRole> action, CancellationToken ct)
    {
        var role = await TenantOperationRole(tenantId, roleId, ct);
        if (role is null) return NotFound();
        if (role.IsSystemRole) return Protected();
        return await MutateLoaded(tenantId, role, action, ct);
    }
    private async Task<Result<RoleDto>> MutateLoaded(Guid tenantId, ApplicationRole role, Action<ApplicationRole> action, CancellationToken ct)
    {
        try { action(role); }
        catch (DomainException ex) { return Fail(ex.Message, RoleErrorCodes.Validation); }
        var updated = await roleManager.UpdateAsync(role);
        return updated.Succeeded ? Result<RoleDto>.Ok(await MapAsync(role, tenantId, ct)) :
            Fail(SafeErrors(updated), RoleErrorCodes.PersistenceFailure);
    }
    private Task<ApplicationRole?> TenantOperationRole(Guid tenantId, Guid roleId, CancellationToken ct) =>
        context.Roles.SingleOrDefaultAsync(x => x.Id == roleId &&
            (x.TenantId == tenantId || x.TenantId == null && x.Type != RoleType.Platform), ct);
    private async Task<(ApplicationUser? User, ApplicationRole? Role, (string Message, string Code)? Error)> LoadAssignment(
        Guid tenantId, Guid userId, Guid roleId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.TenantId != tenantId) return (null, null, ("User was not found.", RoleErrorCodes.UserNotFound));
        var role = await context.Roles.SingleOrDefaultAsync(x => x.Id == roleId &&
            (x.TenantId == tenantId || (x.TenantId == null && x.Type != RoleType.Platform)), ct);
        return role is null ? (user, null, ("Role was not found.", RoleErrorCodes.NotFound)) : (user, role, null);
    }
    private static bool ScopeMatches(ApplicationUser user, ApplicationRole role) => role.Type switch
    {
        RoleType.Platform => user.TenantId is null && user.SchoolId is null && user.CampusId is null,
        RoleType.Tenant => user.TenantId == role.TenantId || role.IsSystemRole && user.TenantId.HasValue,
        RoleType.School => (user.TenantId == role.TenantId || role.IsSystemRole && user.TenantId.HasValue) && user.SchoolId.HasValue,
        RoleType.Campus => (user.TenantId == role.TenantId || role.IsSystemRole && user.TenantId.HasValue) && user.CampusId.HasValue,
        _ => false
    };
    private async Task<RoleDto> MapAsync(ApplicationRole x, Guid tenantId, CancellationToken ct) => new(x.Id, x.TenantId, x.Code,
        x.DisplayName, x.Description, x.Type, x.IsSystemRole, x.IsActive, x.Status, x.SortOrder,
        await (from assignment in context.Set<IdentityUserRole<Guid>>()
            join user in context.Users on assignment.UserId equals user.Id
            where assignment.RoleId == x.Id && user.TenantId == tenantId
            select assignment).CountAsync(ct), x.CreatedAt);
    private static string SafeErrors(IdentityResult result) => string.Join(" ", result.Errors.Select(x => x.Description));
    private static Result<RoleDto> Fail(string message, string code) => Result<RoleDto>.Fail(message, code);
    private static Result<RoleDto> Duplicate() => Fail("A role with this code already exists.", RoleErrorCodes.DuplicateCode);
    private static Result<RoleDto> NotFound() => Fail("Role was not found.", RoleErrorCodes.NotFound);
    private static Result<RoleDto> Protected() => Fail("System roles cannot be modified.", RoleErrorCodes.SystemRoleProtected);
}
