using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolERP.Infrastructure.Persistence;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Seed;

namespace SchoolERP.Api.Extensions;

public static class DatabaseInitializationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!app.Environment.IsDevelopment()) return;
        var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.ApplyMigrationsOnStartup && !options.SeedDevelopmentData) return;
        await using var scope = app.Services.CreateAsyncScope();
        if (options.ApplyMigrationsOnStartup)
            await scope.ServiceProvider.GetRequiredService<SchoolErpDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DevelopmentAdminSeeder>().SeedAsync(cancellationToken);
    }
}
