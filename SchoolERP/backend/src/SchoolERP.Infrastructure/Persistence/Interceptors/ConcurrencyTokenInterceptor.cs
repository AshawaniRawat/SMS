using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Infrastructure.Persistence.Interceptors;

public sealed class ConcurrencyTokenInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateVersions(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateVersions(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void UpdateVersions(DbContext? context)
    {
        if (context is null) return;
        foreach (var entry in context.ChangeTracker.Entries<School>())
        {
            if (entry.State == EntityState.Added) entry.Entity.Version = 1;
            else if (entry.State == EntityState.Modified)
                entry.Entity.Version = entry.Property(entity => entity.Version).OriginalValue + 1;
        }
    }
}
