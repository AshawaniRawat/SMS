using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Interfaces;
using SchoolERP.Application.Exceptions;
using Npgsql;
using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SchoolERP.Infrastructure.Identity;
namespace SchoolERP.Infrastructure.Persistence.Context;
public sealed class SchoolErpDbContext(
    DbContextOptions<SchoolErpDbContext> options,
    ICurrentTenant currentTenant,
    IPlatformDataAccess platformDataAccess) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext, IUnitOfWork
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
        catch (DbUpdateException exception) when (IsCampusCodeConflict(exception)) { throw new CampusCodeConflictException(); }
        catch (DbUpdateException exception) when (IsMainCampusConflict(exception)) { throw new MainCampusConflictException(); }
        catch (DbUpdateException exception) when (IsAcademicYearCodeConflict(exception)) { throw new AcademicYearCodeConflictException(); }
        catch (DbUpdateException exception) when (IsCurrentAcademicYearConflict(exception)) { throw new CurrentAcademicYearConflictException(); }
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
        catch (DbUpdateException exception) when (IsCampusCodeConflict(exception)) { throw new CampusCodeConflictException(); }
        catch (DbUpdateException exception) when (IsMainCampusConflict(exception)) { throw new MainCampusConflictException(); }
        catch (DbUpdateException exception) when (IsAcademicYearCodeConflict(exception)) { throw new AcademicYearCodeConflictException(); }
        catch (DbUpdateException exception) when (IsCurrentAcademicYearConflict(exception)) { throw new CurrentAcademicYearConflictException(); }
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

    private static bool IsCampusCodeConflict(DbUpdateException exception) => exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_campuses_school_id_code" };
    private static bool IsMainCampusConflict(DbUpdateException exception) => exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_campuses_school_id_main" };
    private static bool IsAcademicYearCodeConflict(DbUpdateException exception) => exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_academic_years_school_id_code" };
    private static bool IsCurrentAcademicYearConflict(DbUpdateException exception) => exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_academic_years_school_id_current" };
}
