using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolERP.Application.Features.Tenants;
using SchoolERP.Domain.Enums;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class TenantApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task TenantApiSupportsCreationLookupListingAndLifecycle()
    {
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var slug = "api-" + Guid.NewGuid().ToString("N");
        var request = new CreateTenantRequest("API Tenant", slug, "OWNER@EXAMPLE.COM", "+1 555", "us", "UTC", "usd");

        var createdResponse = await client.PostAsJsonAsync("/api/v1/tenants", request);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await ReadTenant(createdResponse);
        Assert.Equal(TenantStatus.Pending, created.Status); Assert.Equal("owner@example.com", created.Email);

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/tenants", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tenants/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tenants/by-slug/{slug.ToUpperInvariant()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/tenants/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tenants?pageNumber=1&pageSize=10&search={slug}")).StatusCode);

        Assert.Equal(TenantStatus.Active, (await PostStatus(client, created.Id, "activate")).Status);
        Assert.Equal(TenantStatus.Suspended, (await PostStatus(client, created.Id, "suspend")).Status);
        Assert.Equal(TenantStatus.Active, (await PostStatus(client, created.Id, "reactivate")).Status);
        Assert.Equal(TenantStatus.Archived, (await PostStatus(client, created.Id, "archive")).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/v1/tenants/{created.Id}/activate", null)).StatusCode);
    }

    [PostgresFact]
    public async Task TenantApiReturnsSafeValidationErrors()
    {
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/tenants", new CreateTenantRequest("", "admin"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("Npgsql", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<TenantDto> PostStatus(HttpClient client, Guid id, string operation)
    {
        var response = await client.PostAsync($"/api/v1/tenants/{id}/{operation}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadTenant(response);
    }

    private static async Task<TenantDto> ReadTenant(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Deserialize<TenantDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
