using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Infrastructure.Persistence.Interceptors;

public sealed class TenantGuardInterceptor(
    ICurrentTenant currentTenant,
    IPlatformDataAccess platformDataAccess,
    ILogger<TenantGuardInterceptor> logger) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        GuardWrites(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        GuardWrites(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void GuardWrites(DbContext? context)
    {
        if (context is null || platformDataAccess.IsEnabled) return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>().Where(entry =>
                     entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (!currentTenant.IsAvailable || !currentTenant.TenantId.HasValue)
            {
                logger.LogWarning("Tenant-owned write rejected because tenant context is missing.");
                throw new TenantContextMissingException();
            }

            var tenantId = currentTenant.TenantId.Value;
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                entry.Entity.TenantId = tenantId;

            var tenantWasChanged = entry.State != EntityState.Added && entry.Property(nameof(ITenantEntity.TenantId)).IsModified;
            if (entry.Entity.TenantId != tenantId || tenantWasChanged)
            {
                logger.LogWarning("Cross-tenant write rejected for entity type {EntityType}.", entry.Metadata.ClrType.Name);
                throw new TenantMismatchException();
            }
        }
    }
}
