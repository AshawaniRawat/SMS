using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class PersistenceInterceptorTests
{
    [Fact]
    public async Task NewTenantEntityReceivesCurrentTenantId()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId);
        var school = new School { Name = "Assigned", Code = "MAIN" };
        context.Schools.Add(school);
        await context.SaveChangesAsync();
        Assert.Equal(tenantId, school.TenantId);
    }

    [Fact]
    public async Task MismatchedTenantIsRejected()
    {
        await using var context = CreateContext(Guid.NewGuid());
        context.Schools.Add(new School { TenantId = Guid.NewGuid(), Name = "Wrong", Code = "MAIN" });
        await Assert.ThrowsAsync<TenantMismatchException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteBecomesUtcTimestampedSoftDelete()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId);
        var school = new School { TenantId = tenantId, Name = "Deleted", Code = "MAIN", CreatedAt = DateTime.UtcNow.AddDays(-1) };
        context.Attach(school); context.Remove(school);
        await context.SaveChangesAsync();
        Assert.Equal(EntityState.Modified, context.Entry(school).State);
        Assert.True(school.IsDeleted);
        Assert.NotNull(school.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, school.DeletedAt!.Value.Kind);
        Assert.NotNull(school.UpdatedAt);
    }

    private static SchoolErpDbContext CreateContext(Guid? tenantId)
    {
        var tenant = new CurrentTenant(tenantId);
        var platform = new PlatformAccess(false);
        var options = new DbContextOptionsBuilder<SchoolErpDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused")
            .AddInterceptors(
                new TenantGuardInterceptor(tenant, platform, NullLogger<TenantGuardInterceptor>.Instance),
                new SoftDeleteInterceptor(TimeProvider.System),
                new ConcurrencyTokenInterceptor(),
                new AuditableEntityInterceptor(TimeProvider.System),
                new SuppressWritesInterceptor()).Options;
        return new SchoolErpDbContext(options, tenant, platform);
    }

    private sealed record CurrentTenant(Guid? TenantId) : ICurrentTenant { public bool IsAvailable => TenantId.HasValue; }
    private sealed record PlatformAccess(bool IsEnabled) : IPlatformDataAccess;
    private sealed class SuppressWritesInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
            InterceptionResult<int>.SuppressWithResult(1);
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
    }
}
