using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Features.RolePermissions;
using SchoolERP.Application.Security;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace SchoolERP.Infrastructure.Identity;

// Scoped lifetime intentionally makes this a per-request cache.
public sealed class UserPermissionService(SchoolErpDbContext context, IRolePermissionService rolePermissions,
    ICurrentUser currentUser) : IUserPermissionService
{
    private readonly Dictionary<Guid, IReadOnlySet<string>> _cache = [];

    public async Task<IReadOnlySet<string>> GetEffectivePermissionCodesAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(userId, out var cached)) return cached;
        if (!currentUser.IsAuthenticated || currentUser.UserId != userId) return Empty();
        var active = await context.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive &&
            x.Status == UserAccountStatus.Active && x.TenantId == currentUser.TenantId, cancellationToken);
        if (!active) return _cache[userId] = Empty();
        var codes = await rolePermissions.GetUserEffectivePermissionCodesAsync(currentUser.TenantId, userId,
            cancellationToken);
        return _cache[userId] = new HashSet<string>(codes, StringComparer.Ordinal);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode,
        CancellationToken cancellationToken = default) =>
        (await GetEffectivePermissionCodesAsync(userId, cancellationToken)).Contains(permissionCode);

    private static IReadOnlySet<string> Empty() => new HashSet<string>(StringComparer.Ordinal);
}
