using SchoolERP.Application.Common.Pagination;
using SchoolERP.Shared.Results;

namespace SchoolERP.Application.Features.Identity;

public interface IUserIdentityService
{
    Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PagedResult<UserSummaryDto>> GetUsersForTenantAsync(Guid tenantId, PagedRequest page, CancellationToken cancellationToken = default);
    Task<Result<bool>> ActivateUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<bool>> SuspendUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeactivateUserAsync(Guid id, CancellationToken cancellationToken = default);
}
