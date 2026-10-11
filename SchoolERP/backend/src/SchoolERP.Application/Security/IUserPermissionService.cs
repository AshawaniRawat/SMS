namespace SchoolERP.Application.Security;

public interface IUserPermissionService
{
    Task<IReadOnlySet<string>> GetEffectivePermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
}
