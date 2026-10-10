using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Interfaces;
using SchoolERP.Application.Exceptions;
using Npgsql;
using System.Reflection;
namespace SchoolERP.Infrastructure.Persistence.Context;
public sealed class SchoolErpDbContext(
    DbContextOptions<SchoolErpDbContext> options,
    ICurrentTenant currentTenant,
    IPlatformDataAccess platformDataAccess) : DbContext(options), IApplicationDbContext, IUnitOfWork
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Campus> Campuses => Set<Campus>();

    // Properties on the context make the cached EF model use each context's tenant values.
    public Guid? CurrentTenantId => currentTenant.IsAvailable ? currentTenant.TenantId : null;
    public bool HasPlatformAccess => platformDataAccess.IsEnabled;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchoolErpDbContext).Assembly);
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(entity => entity.BaseType is null))
        {
            var tenantOwned = typeof(ITenantEntity).IsAssignableFrom(entity.ClrType);
            var softDeletable = typeof(ISoftDeletableEntity).IsAssignableFrom(entity.ClrType);
            var methodName = (tenantOwned, softDeletable) switch
            {
                (true, true) => nameof(ApplyTenantAndSoftDeleteFilter),
                (true, false) => nameof(ApplyTenantFilter),
                (false, true) => nameof(ApplySoftDeleteFilter),
                _ => null
            };
            if (methodName is not null)
                typeof(SchoolErpDbContext).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(entity.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity =>
            HasPlatformAccess || (CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId));

    private void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ISoftDeletableEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => !entity.IsDeleted);

    private void ApplyTenantAndSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity, ISoftDeletableEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity =>
            !entity.IsDeleted && (HasPlatformAccess || (CurrentTenantId.HasValue && entity.TenantId == CurrentTenantId)));

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try { return base.SaveChanges(acceptAllChangesOnSuccess); }
        catch (DbUpdateConcurrencyException exception) { throw new ConcurrencyConflictException(exception); }
        catch (DbUpdateException exception) when (IsTenantSlugConflict(exception)) { throw new TenantSlugConflictException(); }
        catch (DbUpdateException exception) when (IsSchoolCodeConflict(exception)) { throw new SchoolCodeConflictException(); }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return SaveChangesWithConcurrencyHandlingAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task<int> SaveChangesWithConcurrencyHandlingAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        try { return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
        catch (DbUpdateConcurrencyException exception) { throw new ConcurrencyConflictException(exception); }
        catch (DbUpdateException exception) when (IsTenantSlugConflict(exception)) { throw new TenantSlugConflictException(); }
        catch (DbUpdateException exception) when (IsSchoolCodeConflict(exception)) { throw new SchoolCodeConflictException(); }
    }

    private static bool IsTenantSlugConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_tenants_slug"
        };

    private static bool IsSchoolCodeConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_schools_tenant_id_code"
        };
}
