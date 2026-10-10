using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchoolERP.Application.Abstractions;
using Npgsql;

namespace SchoolERP.IntegrationTests;

public sealed class ApiFactory(string? connectionString = null, string environment = "Development", bool initializeDatabase = false,
    Guid? tenantId = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(DatabaseFailureTestController).Assembly);
            if (tenantId.HasValue)
            {
                services.RemoveAll<ICurrentTenant>();
                services.AddScoped<ICurrentTenant>(_ => new TestCurrentTenant(tenantId));
            }
        });
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString ?? "Host=localhost;Database=unused;Username=unused",
            ["Database:ApplyMigrationsOnStartup"] = initializeDatabase.ToString(),
            ["Database:SeedDevelopmentData"] = initializeDatabase.ToString(),
            ["Jwt:Issuer"] = "SchoolERP.Tests",
            ["Jwt:Audience"] = "SchoolERP.Tests.Client",
            ["Jwt:SigningKey"] = "test-only-signing-key-that-is-at-least-32-bytes-long",
            ["Jwt:AccessTokenLifetimeMinutes"] = "15",
            ["Jwt:ClockSkewSeconds"] = "0"
        }));
    }
}

[Collection(PostgresCollection.Name)]
public sealed class HealthTests(PostgresFixture database)
{
    [PostgresFact] public async Task HealthReturnsHealthy()
    {
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("Healthy", body!["status"]);
    }

    [Fact] public async Task SwaggerIsAvailableInDevelopment()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact] public async Task HealthReturnsSafe503WhenDatabaseIsUnavailable()
    {
        await using var factory = new ApiFactory("Host=127.0.0.1;Port=1;Database=unavailable;Username=unused;Timeout=1;Pooling=false");
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("{\"status\":\"Unhealthy\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact] public async Task DatabaseExceptionResponseDoesNotExposeProviderDetails()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/_tests/database-failure");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Database temporarily unavailable.", body);
        Assert.DoesNotContain("synthetic-private-provider-detail", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public async Task ProductionRefusesStartupMigrationsAndSeedingEvenWhenFlagsAreEnabled()
    {
        await using var factory = new ApiFactory(
            "Host=127.0.0.1;Port=1;Database=unavailable;Username=unused;Timeout=1", "Production", initializeDatabase: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }
}

// Loaded only into WebApplicationFactory. No test endpoints are present in the actual API.
[ApiController]
[Route("_tests/database-failure")]
public sealed class DatabaseFailureTestController : ControllerBase
{
    [HttpGet] public IActionResult Get() => throw new NpgsqlException("synthetic-private-provider-detail");
}
