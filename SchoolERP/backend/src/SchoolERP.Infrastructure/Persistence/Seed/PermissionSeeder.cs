using Microsoft.EntityFrameworkCore;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Security;
using SchoolERP.Infrastructure.Persistence.Context;

namespace SchoolERP.Infrastructure.Persistence.Seed;

public sealed class PermissionSeeder(SchoolErpDbContext context)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var errors = PermissionCatalog.Validate();
        if (errors.Count != 0)
            throw new InvalidOperationException($"Permission catalog is invalid: {string.Join("; ", errors)}");

        var existing = await context.Permissions.ToDictionaryAsync(x => x.Code, StringComparer.Ordinal,
            cancellationToken);
        foreach (var definition in PermissionCatalog.All)
        {
            if (existing.TryGetValue(definition.Code, out var permission)) permission.Synchronize(definition);
            else context.Permissions.Add(Permission.CreateFromDefinition(definition));
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
