using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Persistence.Context;

namespace SchoolERP.Infrastructure.Persistence.Seed;

public sealed class DatabaseSeeder(SchoolErpDbContext context, IOptions<DatabaseOptions> options,
    IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await new PermissionSeeder(context).SeedAsync(cancellationToken);
        await new RolePermissionSeeder(context).SeedAsync(cancellationToken);
        if (!environment.IsDevelopment() || !options.Value.SeedDevelopmentData) return;
        // Only a platform entity is seeded; tenant-owned writes need validated tenant context.
        if (await context.Tenants.AnyAsync(tenant => tenant.Slug == "demo-workspace", cancellationToken)) return;
        context.Tenants.Add(Tenant.Create("Development Demo Workspace", "demo-workspace"));
        await context.SaveChangesAsync(cancellationToken);
    }
}
