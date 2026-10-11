using Microsoft.EntityFrameworkCore;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Security;
using SchoolERP.Infrastructure.Persistence.Seed;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PermissionPersistenceTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task Permission_code_is_globally_unique()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var definition = PermissionCatalog.FindByCode(PermissionCatalog.Student.View)!;
        context.Permissions.Add(Permission.CreateFromDefinition(definition));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        context.Permissions.Add(Permission.CreateFromDefinition(definition with { Id = Guid.NewGuid() }));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task Seeder_is_idempotent_and_synchronizes_metadata_without_deleting_unknown_rows()
    {
        await using var context = database.CreateContext(platformAccess: true);
        var seeder = new PermissionSeeder(context);
        await seeder.SeedAsync();
        var firstCount = await context.Permissions.CountAsync();
        var student = await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Student.View);
        var originalId = student.Id;
        var historical = Permission.CreateFromDefinition(new(Guid.NewGuid(), "Student.LegacyAction", "Legacy action",
            "Student", "LegacyAction", "Historical permission", Domain.Enums.PermissionScope.School, 9999, IsActive: false));
        context.Permissions.Add(historical);
        await context.SaveChangesAsync();

        await seeder.SeedAsync();

        Assert.Equal(firstCount + 1, await context.Permissions.CountAsync());
        Assert.Equal(originalId, (await context.Permissions.SingleAsync(x => x.Code == PermissionCatalog.Student.View)).Id);
        Assert.True(await context.Permissions.AnyAsync(x => x.Code == "Student.LegacyAction"));
        Assert.Equal(1, await context.Permissions.CountAsync(x => x.Code == PermissionCatalog.Student.View));
    }
}
