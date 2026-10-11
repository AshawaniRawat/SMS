using Microsoft.EntityFrameworkCore;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;

namespace SchoolERP.Infrastructure.Persistence.Seed;

public sealed class RolePermissionSeeder(SchoolErpDbContext context)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var roleCodes = SystemRolePermissionSets.All.Keys.ToArray();
        var roles = await context.Roles.Where(x => x.IsSystemRole && roleCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, ct);
        var permissions = await context.Permissions.Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, ct);
        var roleIds = roles.Values.Select(x => x.Id).ToArray();
        var existing = await context.RolePermissions
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => new { x.RoleId, x.PermissionId }).ToListAsync(ct);
        var pairs = existing.Select(x => (x.RoleId, x.PermissionId)).ToHashSet();
        foreach (var set in SystemRolePermissionSets.All)
        {
            if (!roles.TryGetValue(set.Key, out var role)) continue;
            foreach (var code in set.Value.Distinct(StringComparer.Ordinal))
                if (permissions.TryGetValue(code, out var permission) && pairs.Add((role.Id, permission.Id)))
                    context.RolePermissions.Add(RolePermission.Create(role.Id, permission.Id, true));
        }
        await context.SaveChangesAsync(ct);
    }
}
