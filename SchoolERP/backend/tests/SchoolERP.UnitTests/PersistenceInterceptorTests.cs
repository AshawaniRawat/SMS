using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Interceptors;
using Xunit;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.UnitTests;

public sealed class PersistenceInterceptorTests
{
    [Fact]
    public async Task NewTenantEntityReceivesCurrentTenantId()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId);
        var school = School.CreateForCurrentTenant("Assigned", "MAIN");
        context.Schools.Add(school);
        await context.SaveChangesAsync();
        Assert.Equal(tenantId, school.TenantId);
    }

    [Fact]
    public async Task MismatchedTenantIsRejected()
    {
        await using var context = CreateContext(Guid.NewGuid());
        context.Schools.Add(School.Create(Guid.NewGuid(), "Wrong", "MAIN"));
        await Assert.ThrowsAsync<TenantMismatchException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Missing_tenant_rejects_create_and_explicit_platform_requires_target_tenant()
    {
        await using var missing = CreateContext(null);
        missing.Schools.Add(School.CreateForCurrentTenant("Missing", "MISS"));
        await Assert.ThrowsAsync<TenantContextMissingException>(() => missing.SaveChangesAsync());
        await using var platform = CreateContext(null, platformAccess: true);
        platform.Schools.Add(School.CreateForCurrentTenant("Platform Missing", "PMISS"));
        await Assert.ThrowsAsync<TenantContextMissingException>(() => platform.SaveChangesAsync());
    }

    [Fact]
    public async Task Attached_foreign_modified_and_deleted_entities_are_rejected()
    {
        var current = Guid.NewGuid(); var foreign = Guid.NewGuid();
        await using var modified = CreateContext(current);
        var foreignSchool = School.Create(foreign, "Foreign", "FOREIGN");
        modified.Attach(foreignSchool); modified.Entry(foreignSchool).State = EntityState.Modified;
        await Assert.ThrowsAsync<TenantMismatchException>(() => modified.SaveChangesAsync());
        await using var deleted = CreateContext(current);
        var deletedForeign = School.Create(foreign, "Foreign Delete", "FDELETE");
        deleted.Attach(deletedForeign); deleted.Entry(deletedForeign).State = EntityState.Deleted;
        await Assert.ThrowsAsync<TenantMismatchException>(() => deleted.SaveChangesAsync());
    }

    [Fact]
    public void Every_concrete_tenant_entity_has_a_global_query_filter()
    {
        using var context = CreateContext(Guid.NewGuid());
        var tenantTypes = context.Model.GetEntityTypes().Where(x => !x.ClrType.IsAbstract &&
            typeof(ITenantEntity).IsAssignableFrom(x.ClrType)).ToArray();
        Assert.NotEmpty(tenantTypes);
        Assert.All(tenantTypes, x => Assert.NotNull(x.GetQueryFilter()));
        Assert.DoesNotContain(context.Model.GetEntityTypes(), x => x.ClrType == typeof(Tenant) && x.GetQueryFilter() is not null);
    }

    [Fact]
    public async Task DeleteBecomesUtcTimestampedSoftDelete()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId);
        var school = School.Create(tenantId, "Deleted", "MAIN"); school.CreatedAt = DateTime.UtcNow.AddDays(-1);
        context.Attach(school); context.Remove(school);
        await context.SaveChangesAsync();
        Assert.Equal(EntityState.Modified, context.Entry(school).State);
        Assert.True(school.IsDeleted);
        Assert.NotNull(school.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, school.DeletedAt!.Value.Kind);
        Assert.NotNull(school.UpdatedAt);
    }

    private static SchoolErpDbContext CreateContext(Guid? tenantId, bool platformAccess = false)
    {
        var tenant = new CurrentTenant(tenantId);
        var platform = new PlatformAccess(platformAccess);
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
