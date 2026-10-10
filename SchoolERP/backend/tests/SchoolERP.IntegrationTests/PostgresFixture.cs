using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolERP.Application.Abstractions;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Interceptors;
using Testcontainers.PostgreSql;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace SchoolERP.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!PostgresFixture.IsEnabled)
            Skip = "Set SCHOOLERP_TEST_USE_CONTAINERS=true for Testcontainers, or SCHOOLERP_TEST_POSTGRES for an isolated native PostgreSQL test database.";
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "PostgreSQL persistence";
}

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _administrationConnectionString;
    private readonly string _databaseName = "schoolerp_tests_" + Guid.NewGuid().ToString("N");
    private bool _databaseCreated;
    public string ConnectionString { get; private set; } = string.Empty;
    public static bool IsEnabled => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SCHOOLERP_TEST_POSTGRES")) ||
        string.Equals(Environment.GetEnvironmentVariable("SCHOOLERP_TEST_USE_CONTAINERS"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (!IsEnabled) return;
        _administrationConnectionString = Environment.GetEnvironmentVariable("SCHOOLERP_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(_administrationConnectionString))
        {
            _container = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
            await _container.StartAsync();
            _administrationConnectionString = _container.GetConnectionString();
        }
        await using var connection = new NpgsqlConnection(_administrationConnectionString);
        await connection.OpenAsync();
        // Internally generated identifier: never migrate or delete an existing developer database.
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
        _databaseCreated = true;
        ConnectionString = new NpgsqlConnectionStringBuilder(_administrationConnectionString)
        {
            Database = _databaseName, IncludeErrorDetail = false
        }.ConnectionString;
        await using var context = CreateContext(platformAccess: true);
        await context.Database.MigrateAsync();
    }

    public SchoolErpDbContext CreateContext(Guid? tenantId = null, bool platformAccess = false)
    {
        var tenant = new TestCurrentTenant(tenantId);
        var platform = new TestPlatformAccess(platformAccess);
        var options = new DbContextOptionsBuilder<SchoolErpDbContext>().UseNpgsql(ConnectionString)
            .AddInterceptors(
                new TenantGuardInterceptor(tenant, platform, NullLogger<TenantGuardInterceptor>.Instance),
                new SoftDeleteInterceptor(TimeProvider.System),
                new ConcurrencyTokenInterceptor(),
                new AuditableEntityInterceptor(TimeProvider.System)).Options;
        return new SchoolErpDbContext(options, tenant, platform);
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_databaseCreated && _administrationConnectionString is not null)
            {
                NpgsqlConnection.ClearAllPools();
                await using var connection = new NpgsqlConnection(_administrationConnectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand($"DROP DATABASE \"{_databaseName}\" WITH (FORCE)", connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        finally { if (_container is not null) await _container.DisposeAsync(); }
    }
}

internal sealed record TestCurrentTenant(Guid? TenantId) : ICurrentTenant
{
    public bool IsAvailable => TenantId.HasValue;
}
internal sealed record TestPlatformAccess(bool IsEnabled) : IPlatformDataAccess;
