using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolERP.Application.Abstractions;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class AuditTests
{
    private static SchoolErpDbContext CreateContext()
    {
        // Suppress database writes to test the audit hook, without simulating PostgreSQL behavior.
        var options = new DbContextOptionsBuilder<SchoolErpDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused")
            .AddInterceptors(new AuditableEntityInterceptor(TimeProvider.System), new SuppressWritesInterceptor()).Options;
        return new SchoolErpDbContext(options, new MissingTenant(), new NoPlatformAccess());
    }

    [Fact] public void SyncSaveSetsCreatedAtInUtc()
    {
        using var context = CreateContext();
        var tenant = Tenant.Create("Audit", "audit"); tenant.CreatedAt = DateTime.MinValue;
        context.Tenants.Add(tenant); var before = DateTime.UtcNow;
        context.SaveChanges();
        Assert.InRange(tenant.CreatedAt, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, tenant.CreatedAt.Kind); Assert.Null(tenant.UpdatedAt);
    }

    [Fact] public async Task AsyncSaveSetsUpdatedAtAndPreservesCreationFields()
    {
        await using var context = CreateContext();
        var createdAt = DateTime.UtcNow.AddDays(-2); var creator = Guid.NewGuid();
        var tenant = Tenant.Create("Audit", "audit"); tenant.CreatedAt = createdAt; tenant.CreatedBy = creator;
        context.Attach(tenant);
        tenant.UpdateProfile("Changed", null, null, null, null, null, null, null, null);
        tenant.CreatedAt = DateTime.MinValue; tenant.CreatedBy = Guid.NewGuid();
        var before = DateTime.UtcNow;
        await context.SaveChangesAsync();
        Assert.Equal(createdAt, tenant.CreatedAt); Assert.Equal(creator, tenant.CreatedBy);
        Assert.InRange(tenant.UpdatedAt!.Value, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, tenant.UpdatedAt.Value.Kind);
    }

    [Fact] public async Task UnchangedEntityDoesNotReceiveUpdatedAt()
    {
        await using var context = CreateContext();
        var tenant = Tenant.Create("Audit", "audit"); context.Attach(tenant);
        await context.SaveChangesAsync(); Assert.Null(tenant.UpdatedAt);
    }

    private sealed class MissingTenant : ICurrentTenant { public Guid? TenantId => null; public bool IsAvailable => false; }
    private sealed class NoPlatformAccess : IPlatformDataAccess { public bool IsEnabled => false; }
    private sealed class SuppressWritesInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) => InterceptionResult<int>.SuppressWithResult(0);
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
    }
}
