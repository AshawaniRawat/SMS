using System.Net;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Api.ErrorHandling;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class IntegrationInfrastructureTests(PostgresFixture database) : IntegrationTestBase(database)
{
    [PostgresFact]
    [Trait("Category", "Persistence")]
    public async Task MigrateAsync_WhenSchemaIsCurrent_IsIdempotent()
    {
        await MigrateAgainAsync();
        var migrations = await ExecuteDbContextAsync(context =>
            context.Database.GetAppliedMigrationsAsync());
        Assert.NotEmpty(migrations);
    }

    [PostgresFact]
    [Trait("Category", "Infrastructure")]
    public async Task Health_WhenDatabaseIsAvailable_ReturnsHealthyAndGeneratedCorrelationId()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(values)));
    }

    [PostgresFact]
    [Trait("Category", "Infrastructure")]
    public async Task Request_WithSafeCorrelationId_PropagatesSameValue()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient().WithCorrelationId("integration-123");
        var response = await client.GetAsync("/api/health");
        Assert.Equal("integration-123", Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
    }

    [PostgresFact]
    [Trait("Category", "Authentication")]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsStandard401Contract()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        await (await client.GetAsync("/api/v1/campuses"))
            .ShouldBeApiErrorAsync(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [PostgresFact]
    [Trait("Category", "Authentication")]
    public async Task ProtectedEndpoint_WithInvalidSignature_ReturnsSafe401Contract()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient().WithBearerToken(TestJwtTokenFactory.Create(
            new TestUserSpec(Guid.NewGuid(), "invalid@example.com", Guid.NewGuid()),
            signingKey: "different-test-signing-key-that-is-long-enough-12345"));
        var response = await client.GetAsync("/api/v1/campuses");
        var body = await response.ShouldBeApiErrorAsync(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
        Assert.DoesNotContain("signature", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [PostgresFact]
    [Trait("Category", "OpenApi")]
    public async Task OpenApi_InTestingHost_ExposesBearerAndNoTestSecret()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"Bearer\"", json);
        Assert.DoesNotContain(TestJwtTokenFactory.SigningKey, json, StringComparison.Ordinal);
    }
}
