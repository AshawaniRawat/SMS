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
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Security.Claims;
using SchoolERP.Application.Security;
using SchoolERP.Domain.Security;
using System.Text.Json;

namespace SchoolERP.IntegrationTests;

public sealed class ApiFactory(string? connectionString = null, string environment = "Development", bool initializeDatabase = false,
    Guid? tenantId = null, Action<IServiceCollection>? configureServices = null, bool authorizePlatform = false) : WebApplicationFactory<Program>
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
            if (tenantId.HasValue || authorizePlatform)
            {
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                });
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName, _ => { });
                services.RemoveAll<IUserPermissionService>();
                services.AddScoped<IUserPermissionService>(_ => new AllPermissionsTestService());
            }
            configureServices?.Invoke(services);
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

internal sealed class AllPermissionsTestService : IUserPermissionService
{
    private static readonly IReadOnlySet<string> Codes = new HashSet<string>(
        PermissionCatalog.All.Select(x => x.Code), StringComparer.Ordinal);
    public Task<IReadOnlySet<string>> GetEffectivePermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(Codes);
    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default) => Task.FromResult(Codes.Contains(permissionCode));
}

internal sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationTest";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenantId = Context.RequestServices.GetRequiredService<ICurrentTenant>().TenantId;
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.Parse("11111111-1111-1111-1111-111111111111").ToString()) };
        if (tenantId.HasValue) claims.Add(new(SchoolERP.Application.Security.CustomClaimTypes.TenantId, tenantId.Value.ToString()));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName)));
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
        var document = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
        var json = await document.Content.ReadAsStringAsync();
        using var parsed=JsonDocument.Parse(json);var root=parsed.RootElement;
        Assert.Equal("3.0.4",root.GetProperty("openapi").GetString());
        Assert.Equal("SchoolERP API v1",root.GetProperty("info").GetProperty("title").GetString());
        var bearer=root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http",bearer.GetProperty("type").GetString());Assert.Equal("bearer",bearer.GetProperty("scheme").GetString());
        var paths=root.GetProperty("paths");Assert.False(paths.GetProperty("/api/v1/auth/login").GetProperty("post").TryGetProperty("security",out _));
        var campus=paths.GetProperty("/api/v1/campuses/{id}").GetProperty("put");Assert.True(campus.TryGetProperty("security",out _));Assert.Equal("Campus.Edit",campus.GetProperty("x-required-permissions")[0].GetString());
        Assert.True(campus.GetProperty("requestBody").GetProperty("content").TryGetProperty("application/json",out _));
        var errorSchema=campus.GetProperty("responses").GetProperty("400").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString();Assert.Contains("ApiErrorResponse",errorSchema);
        var calendar=root.GetProperty("components").GetProperty("schemas").GetProperty("SchoolERP.Application.Features.SchoolCalendar.CalendarEventRequest");Assert.Equal("time",calendar.GetProperty("properties").GetProperty("startTime").GetProperty("format").GetString());
        var auditParameters=paths.GetProperty("/api/v1/audit-logs").GetProperty("get").GetProperty("parameters");Assert.Contains(auditParameters.EnumerateArray(),x=>x.GetProperty("name").GetString()=="pageNumber");Assert.Contains(auditParameters.EnumerateArray(),x=>x.GetProperty("name").GetString()=="pageSize");
        Assert.DoesNotContain("PasswordHash",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("SecurityStamp",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("TokenHash",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("test-only-signing-key",json,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
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
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }
}

// Loaded only into WebApplicationFactory. No test endpoints are present in the actual API.
[ApiController]
[Route("_tests/database-failure")]
public sealed class DatabaseFailureTestController : ControllerBase
{
    [HttpGet] public IActionResult Get() => throw new NpgsqlException("synthetic-private-provider-detail");
}
