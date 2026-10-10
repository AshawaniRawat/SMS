using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SchoolERP.Infrastructure.Persistence;
using SchoolERP.Infrastructure.Persistence.Seed;
using SchoolERP.Application.Common.Extensions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Enums;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PersistenceTests(PostgresFixture database)
{
    private static Tenant NewTenant() => Tenant.Create("Integration Test Tenant", "test-" + Guid.NewGuid().ToString("N"));
    private static School NewSchool(Tenant tenant, string code = "MAIN") => School.Create(tenant, "Test School", code);
    private static Campus NewCampus(School school, string code = "MAIN") => Campus.Create(school, "Test Campus", code);

    [PostgresFact] public async Task CanConnectAndInitialMigrationCreatesFoundationTables()
    {
        await using var context = database.CreateContext();
        Assert.True(await context.Database.CanConnectAsync());
        Assert.Contains(await context.Database.GetAppliedMigrationsAsync(), migration => migration.EndsWith("InitialDatabaseSetup", StringComparison.Ordinal));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        foreach (var table in new[] { "tenants", "schools", "campuses", "__EFMigrationsHistory" }) Assert.Contains(table, tables);
    }

    [PostgresFact] public async Task InsertsTenantSchoolCampusAndPreservesAuditOnUpdate()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var tenant = NewTenant(); var school = NewSchool(tenant); var campus = NewCampus(school);
        context.Campuses.Add(campus); await context.SaveChangesAsync();
        Assert.NotEqual(Guid.Empty, tenant.Id); Assert.Equal(DateTimeKind.Utc, school.CreatedAt.Kind);
        var createdAt = school.CreatedAt;
        UpdateSchoolName(school, "Updated School"); school.CreatedAt = DateTime.UtcNow.AddYears(-1);
        await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var saved = await context.Schools.SingleAsync(entity => entity.Id == school.Id);
        Assert.Equal(createdAt.Ticks / 10, saved.CreatedAt.Ticks / 10); // PostgreSQL microsecond precision.
        Assert.NotNull(saved.UpdatedAt); Assert.Equal(DateTimeKind.Utc, saved.UpdatedAt!.Value.Kind);
        var savedCampus = await context.Campuses.Include(entity => entity.School).SingleAsync(entity => entity.Id == campus.Id);
        Assert.Equal(tenant.Id, savedCampus.TenantId); Assert.Equal(school.Id, savedCampus.School.Id);
    }

    [PostgresFact] public async Task DuplicateTenantSlugIsRejected()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var tenant = NewTenant(); context.Tenants.Add(tenant); await context.SaveChangesAsync();
        context.Tenants.Add(Tenant.Create("Duplicate", tenant.Slug));
        await Assert.ThrowsAsync<TenantSlugConflictException>(() => context.SaveChangesAsync());
    }

    [PostgresFact] public async Task DuplicateSchoolCodeInsideTenantIsRejected()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var tenant = NewTenant(); context.Schools.Add(NewSchool(tenant)); await context.SaveChangesAsync();
        context.Schools.Add(NewSchool(tenant)); await Assert.ThrowsAsync<SchoolCodeConflictException>(() => context.SaveChangesAsync());
    }

    [PostgresFact] public async Task SameSchoolCodeAcrossTenantsIsAllowed()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var first = NewSchool(NewTenant()); var second = NewSchool(NewTenant());
        context.Schools.AddRange(first, second); await context.SaveChangesAsync();
        Assert.Equal(2, await context.Schools.CountAsync(entity => entity.Id == first.Id || entity.Id == second.Id));
    }

    [PostgresFact] public async Task DuplicateCampusCodeInsideSchoolIsRejected()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var school = NewSchool(NewTenant()); context.Campuses.Add(NewCampus(school)); await context.SaveChangesAsync();
        context.Campuses.Add(NewCampus(school)); await Assert.ThrowsAsync<CampusCodeConflictException>(() => context.SaveChangesAsync());
    }

    [PostgresFact] public async Task SameCampusCodeAcrossSchoolsIsAllowed()
    {
        await using var context = database.CreateContext(platformAccess: true);
        context.Campuses.AddRange(NewCampus(NewSchool(NewTenant())), NewCampus(NewSchool(NewTenant())));
        await context.SaveChangesAsync();
    }

    [PostgresFact] public async Task CampusCannotReferenceSchoolFromAnotherTenant()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var school = NewSchool(NewTenant()); var otherTenant = NewTenant();
        context.Schools.Add(school); context.Tenants.Add(otherTenant); await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        context.Campuses.Add(Campus.Create(otherTenant.Id, school.Id, "Invalid", "MAIN"));
        await AssertPostgresFailure(context, PostgresErrorCodes.ForeignKeyViolation);
    }

    [PostgresFact] public async Task TenantArchivePreservesChildren()
    {
        var tenant = NewTenant(); var school = NewSchool(tenant); var campus = NewCampus(school);
        await using (var setup = database.CreateContext(platformAccess: true)) { setup.Campuses.Add(campus); await setup.SaveChangesAsync(); }
        await using var context = database.CreateContext(platformAccess: true);
        var savedTenant = await context.Tenants.SingleAsync(entity => entity.Id == tenant.Id);
        savedTenant.Archive(DateTime.UtcNow);
        await context.SaveChangesAsync();
        Assert.Equal(SchoolERP.Domain.Enums.TenantStatus.Archived,
            (await context.Tenants.SingleAsync(entity => entity.Id == tenant.Id)).Status);
        Assert.Equal(1, await context.Schools.IgnoreQueryFilters().CountAsync(entity => entity.Id == school.Id));
    }

    [PostgresFact] public async Task QueriesAreIsolatedPerContextAndMissingTenantReturnsNoRows()
    {
        var first = NewSchool(NewTenant()); var second = NewSchool(NewTenant());
        await using (var setup = database.CreateContext(platformAccess: true))
        {
            setup.Campuses.AddRange(NewCampus(first), NewCampus(second)); await setup.SaveChangesAsync();
        }
        await using var missing = database.CreateContext();
        Assert.Empty(await missing.Schools.ToListAsync()); Assert.Empty(await missing.Campuses.ToListAsync());
        await using var firstContext = database.CreateContext(first.TenantId);
        Assert.Equal(first.Id, (await firstContext.Schools.SingleAsync()).Id);
        Assert.All(await firstContext.Campuses.Include(entity => entity.School).ToListAsync(), entity => Assert.Equal(first.TenantId, entity.TenantId));
        await using var secondContext = database.CreateContext(second.TenantId);
        Assert.Equal(second.Id, (await secondContext.Schools.SingleAsync()).Id);
    }

    [PostgresFact] public async Task MissingTenantAndCrossTenantWritesAreRejected()
    {
        await using var missing = database.CreateContext();
        missing.Schools.Add(School.Create(Guid.NewGuid(), "Invalid", "MAIN"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => missing.SaveChangesAsync());
        await using var mismatched = database.CreateContext(Guid.NewGuid());
        mismatched.Schools.Add(School.Create(Guid.NewGuid(), "Invalid", "MAIN"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => mismatched.SaveChangesAsync());
    }

    [PostgresFact] public async Task ValidTenantContextAllowsTenantOwnedWrites()
    {
        var tenant = NewTenant();
        await using (var setup = database.CreateContext()) { setup.Tenants.Add(tenant); await setup.SaveChangesAsync(); }
        await using var context = database.CreateContext(tenant.Id);
        var school = School.Create(tenant.Id, "Scoped School", "MAIN");
        context.Schools.Add(school); await context.SaveChangesAsync();
        Assert.Equal(school.Id, (await context.Schools.SingleAsync()).Id);
    }

    [PostgresFact] public async Task EmptyTenantIdIsAssignedFromCurrentTenant()
    {
        var tenant = NewTenant();
        await using (var setup = database.CreateContext(platformAccess: true)) { setup.Tenants.Add(tenant); await setup.SaveChangesAsync(); }
        await using var context = database.CreateContext(tenant.Id);
        var school = School.CreateForCurrentTenant("Assigned School", "ASSIGNED");
        context.Schools.Add(school); await context.SaveChangesAsync();
        Assert.Equal(tenant.Id, school.TenantId);
    }

    [PostgresFact] public async Task TenantMismatchIsPredictableForAddUpdateAndDelete()
    {
        var school = NewSchool(NewTenant());
        await using (var setup = database.CreateContext(platformAccess: true)) { setup.Schools.Add(school); await setup.SaveChangesAsync(); }

        await using var add = database.CreateContext(Guid.NewGuid());
        add.Schools.Add(School.Create(school.TenantId, "Wrong", "WRONG"));
        await Assert.ThrowsAsync<TenantMismatchException>(() => add.SaveChangesAsync());

        await using var update = database.CreateContext(Guid.NewGuid());
        update.Attach(school); UpdateSchoolName(school, "Cross tenant update");
        await Assert.ThrowsAsync<TenantMismatchException>(() => update.SaveChangesAsync());

        await using var delete = database.CreateContext(Guid.NewGuid());
        delete.Remove(school);
        await Assert.ThrowsAsync<TenantMismatchException>(() => delete.SaveChangesAsync());
    }

    [PostgresFact] public async Task TenantAndSoftDeleteFiltersAreComposedAndPlatformScopeIsExplicit()
    {
        var first = NewSchool(NewTenant(), "FIRST"); var second = NewSchool(NewTenant(), "SECOND");
        await using (var setup = database.CreateContext(platformAccess: true))
        {
            setup.Campuses.AddRange(NewCampus(first), NewCampus(second)); await setup.SaveChangesAsync();
        }
        await using (var scoped = database.CreateContext(first.TenantId))
        {
            scoped.Remove(await scoped.Schools.SingleAsync()); await scoped.SaveChangesAsync();
            Assert.Empty(await scoped.Schools.ToListAsync());
            var deleted = await scoped.Schools.IgnoreQueryFilters().SingleAsync(entity => entity.Id == first.Id);
            Assert.True(deleted.IsDeleted); Assert.NotNull(deleted.DeletedAt);
        }
        await using var other = database.CreateContext(second.TenantId);
        Assert.Equal(second.Id, (await other.Schools.SingleAsync()).Id);
        Assert.All(await other.Campuses.ToListAsync(), campus => Assert.Equal(second.TenantId, campus.TenantId));
        await using var platform = database.CreateContext(platformAccess: true);
        Assert.DoesNotContain(await platform.Schools.ToListAsync(), entity => entity.Id == first.Id);
        Assert.Contains(await platform.Schools.ToListAsync(), entity => entity.Id == second.Id);
    }

    [PostgresFact] public async Task SchoolConcurrencyConflictIsWrapped()
    {
        var school = NewSchool(NewTenant());
        await using (var setup = database.CreateContext(platformAccess: true)) { setup.Schools.Add(school); await setup.SaveChangesAsync(); }
        await using var first = database.CreateContext(platformAccess: true);
        await using var second = database.CreateContext(platformAccess: true);
        var firstCopy = await first.Schools.SingleAsync(entity => entity.Id == school.Id);
        var secondCopy = await second.Schools.SingleAsync(entity => entity.Id == school.Id);
        UpdateSchoolName(firstCopy, "First update"); await first.SaveChangesAsync();
        UpdateSchoolName(secondCopy, "Second update");
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => second.SaveChangesAsync());
    }

    [PostgresFact] public async Task PaginationReportsCountsNavigationAndEmptyPages()
    {
        var tenant = NewTenant();
        await using (var setup = database.CreateContext(platformAccess: true))
        {
            setup.Schools.AddRange(Enumerable.Range(1, 5).Select(index => NewSchool(tenant, $"S{index}")));
            await setup.SaveChangesAsync();
        }
        await using var context = database.CreateContext(tenant.Id);
        var secondPage = await context.Schools.AsNoTracking().OrderBy(entity => entity.Code)
            .ToPagedResultAsync(new PagedRequest(2, 2));
        Assert.Equal(5, secondPage.TotalCount); Assert.Equal(3, secondPage.TotalPages);
        Assert.Equal(2, secondPage.Items.Count); Assert.True(secondPage.HasPreviousPage); Assert.True(secondPage.HasNextPage);
        var emptyPage = await context.Schools.AsNoTracking().OrderBy(entity => entity.Code)
            .ToPagedResultAsync(new PagedRequest(4, 2));
        Assert.Empty(emptyPage.Items); Assert.False(emptyPage.HasNextPage);
    }

    [PostgresFact] public async Task TenantLifecycleProfileAndSchemaPersistWithoutTenantContext()
    {
        var tenant = Tenant.Create(" Lifecycle Tenant ", "lifecycle-" + Guid.NewGuid().ToString("N"),
            "OWNER@EXAMPLE.COM", "+1 555", "us", "UTC");
        tenant.UpdateProfile(tenant.Name, "Lifecycle Legal", tenant.Email, tenant.Phone, "https://example.com",
            "https://example.com/logo.png", tenant.CountryCode, tenant.TimeZoneId, "usd");
        await using (var context = database.CreateContext())
        {
            context.Tenants.Add(tenant); await context.SaveChangesAsync();
            tenant.Activate(DateTime.UtcNow); await context.SaveChangesAsync();
            tenant.Suspend(DateTime.UtcNow); await context.SaveChangesAsync();
        }
        await using var read = database.CreateContext();
        var saved = await read.Tenants.AsNoTracking().SingleAsync(entity => entity.Slug == tenant.Slug);
        Assert.Equal(TenantStatus.Suspended, saved.Status);
        Assert.Equal("owner@example.com", saved.Email); Assert.Equal("US", saved.CountryCode); Assert.Equal("USD", saved.DefaultCurrency);
        Assert.Equal(1, (await read.Tenants.AsNoTracking().Where(entity => entity.Id == tenant.Id)
            .ToPagedResultAsync(new PagedRequest(1, 10))).TotalCount);
    }

    [PostgresFact] public async Task DatabaseRejectsInvalidSlugEvenWhenBypassingDomainValidation()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var tenant = NewTenant(); context.Tenants.Add(tenant); await context.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE tenants SET slug = {"UPPERCASE"} WHERE id = {tenant.Id}"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    private static async Task AssertPostgresFailure(SchoolErpDbContext context, string sqlState)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(sqlState, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    private static void UpdateSchoolName(School school, string name) => school.UpdateProfile(
        name, school.ShortName, school.RegistrationNumber, school.AffiliationNumber, school.BoardCode, school.BoardType,
        school.Email, school.Phone, school.AlternatePhone, school.Website, school.AddressLine1, school.AddressLine2,
        school.City, school.State, school.PostalCode, school.CountryCode, school.LogoUrl, school.TimeZoneId,
        school.DefaultCurrency, school.PrincipalName);

    [PostgresFact] public async Task DemoSeedRequiresExplicitDevelopmentFlagAndIsIdempotent()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var tenantsBefore = await context.Tenants.CountAsync();
        await new DatabaseSeeder(context, Options.Create(new DatabaseOptions { SeedDevelopmentData = true }), new TestEnvironment("Production")).SeedAsync();
        await new DatabaseSeeder(context, Options.Create(new DatabaseOptions()), new TestEnvironment("Development")).SeedAsync();
        Assert.Equal(tenantsBefore, await context.Tenants.CountAsync());
        var seeder = new DatabaseSeeder(context, Options.Create(new DatabaseOptions { SeedDevelopmentData = true }), new TestEnvironment("Development"));
        await seeder.SeedAsync(); await seeder.SeedAsync();
        Assert.Equal(1, await context.Tenants.CountAsync(tenant => tenant.Slug == "demo-workspace"));
    }

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "SchoolERP.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
