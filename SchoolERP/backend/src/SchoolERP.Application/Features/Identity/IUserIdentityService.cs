using SchoolERP.Application.Common.Pagination;
using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.Identity;

public interface IUserIdentityService
{
    Task<Result<UserSummaryDto>> CreatePlatformUserAsync(CreatePlatformUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserSummaryDto>> CreateTenantUserAsync(CreateTenantUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> GetTenantUserByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PagedResult<UserSummaryDto>> GetUsersForTenantAsync(Guid tenantId, PagedRequest page, CancellationToken cancellationToken = default);
    Task<Result<bool>> ActivateTenantUserAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<Result<bool>> SuspendTenantUserAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeactivateTenantUserAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
