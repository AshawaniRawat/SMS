using Microsoft.EntityFrameworkCore;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Seed;
using Xunit;

namespace SchoolERP.IntegrationTests;

public abstract class IntegrationTestBase(PostgresFixture database) : IAsyncLifetime
{
    protected PostgresFixture Database { get; } = database;

    public async Task InitializeAsync()
    {
        await Database.ResetDatabaseAsync();
        await ExecuteDbContextAsync(async context =>
        {
            await new PermissionSeeder(context).SeedAsync();
            await new RolePermissionSeeder(context).SeedAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task ExecuteDbContextAsync(Func<SchoolErpDbContext, Task> action)
    {
        await using var context = Database.CreateContext(platformAccess: true);
        await action(context);
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<SchoolErpDbContext, Task<T>> action)
    {
        await using var context = Database.CreateContext(platformAccess: true);
        return await action(context);
    }

    protected SchoolErpWebApplicationFactory CreateFactory() => new(Database.ConnectionString);

    protected async Task MigrateAgainAsync() =>
        await ExecuteDbContextAsync(context => context.Database.MigrateAsync());
}
