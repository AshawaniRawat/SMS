using Npgsql;
using Respawn;

namespace SchoolERP.IntegrationTests;

public sealed class DatabaseResetter
{
    private readonly string _connectionString;
    private readonly Respawner _respawner;

    private DatabaseResetter(string connectionString, Respawner respawner)
    {
        _connectionString = connectionString;
        _respawner = respawner;
    }

    public static async Task<DatabaseResetter> CreateAsync(string connectionString,
        CancellationToken cancellationToken = default)
    {
        GuardTestDatabase(connectionString);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
            WithReseed = true
        });
        return new DatabaseResetter(connectionString, respawner);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        GuardTestDatabase(_connectionString);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await _respawner.ResetAsync(connection);
    }

    private static void GuardTestDatabase(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database ?? string.Empty;
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase) &&
            !database.Contains("integration", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Database reset refused: database name must contain 'test' or 'integration'.");
    }
}
