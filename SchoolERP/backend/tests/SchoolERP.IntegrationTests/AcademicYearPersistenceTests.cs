using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolERP.Domain.Entities;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AcademicYearPersistenceTests(PostgresFixture database)
{
    [PostgresFact] public async Task TenantFilterAndCompositeSchoolOwnershipAreEnforced()
    {
        var firstTenant = Tenant.Create("AY First", "ay-first-" + Guid.NewGuid().ToString("N"));
        var secondTenant = Tenant.Create("AY Second", "ay-second-" + Guid.NewGuid().ToString("N"));
        var firstSchool = School.Create(firstTenant, "First School", "FIRST"); var secondSchool = School.Create(secondTenant, "Second School", "SECOND");
        var first = AcademicYear.Create(firstSchool, "2026-27", "AY2026", new(2026, 4, 1), new(2027, 3, 31));
        var second = AcademicYear.Create(secondSchool, "2026-27", "AY2026", new(2026, 4, 1), new(2027, 3, 31));
        await using (var setup = database.CreateContext(platformAccess: true)) { setup.AcademicYears.AddRange(first, second); await setup.SaveChangesAsync(); }
        await using var firstContext = database.CreateContext(firstTenant.Id); Assert.Equal(first.Id, (await firstContext.AcademicYears.SingleAsync()).Id);
        await using var missing = database.CreateContext(); Assert.Empty(await missing.AcademicYears.ToListAsync());
        await using var invalid = database.CreateContext(platformAccess: true); invalid.AcademicYears.Add(AcademicYear.Create(firstTenant.Id, secondSchool.Id, "Invalid", "INVALID", new(2028, 4, 1), new(2029, 3, 31)));
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => invalid.SaveChangesAsync()); Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    [PostgresFact] public async Task PartialIndexPreventsTwoCurrentYearsForSchool()
    {
        var tenant = Tenant.Create("AY Current", "ay-current-" + Guid.NewGuid().ToString("N")); var school = School.Create(tenant, "School", "S");
        var first = AcademicYear.Create(school, "One", "ONE", new(2026, 1, 1), new(2026, 12, 31));
        var second = AcademicYear.Create(school, "Two", "TWO", new(2027, 1, 1), new(2027, 12, 31));
        await using var context = database.CreateContext(platformAccess: true); context.AcademicYears.AddRange(first, second); await context.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync($"UPDATE academic_years SET is_current = true WHERE id IN ({first.Id}, {second.Id})"));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }
}
