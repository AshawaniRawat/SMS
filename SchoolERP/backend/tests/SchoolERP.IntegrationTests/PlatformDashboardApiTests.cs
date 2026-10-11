using System.Net;
using System.Net.Http.Json;
using SchoolERP.Domain.Entities;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PlatformDashboardApiTests(PostgresFixture database)
{
    [Fact]
    public async Task Tenant_user_is_denied_before_platform_queries_execute()
    {
        await using var factory = new ApiFactory(tenantId: Guid.NewGuid());
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/v1/platform/dashboard")).StatusCode);
    }

    [PostgresFact]
    public async Task Authorized_platform_dashboard_aggregates_across_tenants()
    {
        var first = Tenant.Create("Dashboard First", $"dashboard-first-{Guid.NewGuid():N}");
        first.Activate(DateTime.UtcNow);
        var second = Tenant.Create("Dashboard Second", $"dashboard-second-{Guid.NewGuid():N}");
        await using (var context = database.CreateContext(platformAccess: true))
        {
            context.Tenants.AddRange(first, second);
            await context.SaveChangesAsync();
        }
        await using var factory = new ApiFactory(database.ConnectionString, authorizePlatform: true);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/platform/dashboard?trendMonths=12&recentTenantLimit=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(json.GetProperty("data").GetProperty("summary").GetProperty("totalTenants").GetInt32() >= 2);
        Assert.Equal(12, json.GetProperty("data").GetProperty("onboardingTrend").GetArrayLength());
    }
}
