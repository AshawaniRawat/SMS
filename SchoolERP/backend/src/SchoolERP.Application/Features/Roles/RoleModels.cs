using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.Roles;

public sealed record RoleDto(Guid Id, Guid? TenantId, string Code, string DisplayName, string? Description,
    RoleType Type, bool IsSystemRole, bool IsActive, RoleStatus Status, int? SortOrder,
    int AssignedUsersCount, DateTime CreatedAt);

public sealed record CreateTenantRoleRequest(string Code, string DisplayName, string? Description,
    RoleType Type, int? SortOrder = null);
public sealed record UpdateRoleRequest(string DisplayName, string? Description, int? SortOrder = null);

public interface IRoleManagementService
{
    Task<Result<RoleDto>> CreateTenantRoleAsync(Guid tenantId, CreateTenantRoleRequest request, CancellationToken ct = default);
    Task<PagedResult<RoleDto>> ListTenantRolesAsync(Guid tenantId, PagedRequest page, RoleStatus? status = null,
        RoleType? type = null, bool? isSystemRole = null, string? search = null, CancellationToken ct = default);
    Task<RoleDto?> GetTenantVisibleRoleAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task<Result<RoleDto>> UpdateTenantRoleAsync(Guid tenantId, Guid roleId, UpdateRoleRequest request, CancellationToken ct = default);
    Task<Result<RoleDto>> ActivateAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task<Result<RoleDto>> DeactivateAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task<Result<RoleDto>> ArchiveAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task<Result<bool>> AssignRoleAsync(Guid tenantId, Guid userId, Guid roleId, CancellationToken ct = default);
    Task<Result<bool>> RemoveRoleAsync(Guid tenantId, Guid userId, Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyCollection<RoleDto>> GetUserRolesAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
}

public static class RoleErrorCodes
{
    public const string Validation = "Role.Validation";
    public const string DuplicateCode = "Role.DuplicateCode";
    public const string NotFound = "Role.NotFound";
    public const string SystemRoleProtected = "Role.SystemRoleProtected";
    public const string HasAssignedUsers = "Role.HasAssignedUsers";
    public const string Inactive = "Role.Inactive";
    public const string ScopeMismatch = "Role.ScopeMismatch";
    public const string UserNotFound = "Role.UserNotFound";
    public const string PersistenceFailure = "Role.PersistenceFailure";
}
