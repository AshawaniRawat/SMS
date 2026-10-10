using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolERP.Application.Features.Campuses;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class CampusApiTests(PostgresFixture database)
{
    [PostgresFact] public async Task CampusApiSupportsHierarchyMainSwitchLifecycleAndIsolation()
    {
        var tenant = Tenant.Create("Campus API", "campus-api-" + Guid.NewGuid().ToString("N")); tenant.Activate(DateTime.UtcNow);
        var school = School.Create(tenant, "Campus School", "CS"); school.Activate(DateTime.UtcNow);
        var otherTenant = Tenant.Create("Other", "campus-other-" + Guid.NewGuid().ToString("N"));
        await using (var setup = database.CreateContext(platformAccess: true)) { setup.Schools.Add(school); setup.Tenants.Add(otherTenant); await setup.SaveChangesAsync(); }

        await using var factory = new ApiFactory(database.ConnectionString, tenantId: tenant.Id); using var client = factory.CreateClient();
        var firstResponse = await client.PostAsJsonAsync("/api/v1/campuses", new CreateCampusRequest(school.Id, "Main Campus", "main"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode); var first = await Read(firstResponse); Assert.True(first.IsMainCampus);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/campuses", new CreateCampusRequest(school.Id, "Duplicate", "MAIN"))).StatusCode);
        var secondResponse = await client.PostAsJsonAsync("/api/v1/campuses", new CreateCampusRequest(school.Id, "North", "NORTH"));
        var second = await Read(secondResponse); Assert.False(second.IsMainCampus);
        second = await Read(await client.PostAsync($"/api/v1/campuses/{second.Id}/set-main", null)); Assert.True(second.IsMainCampus);
        Assert.Equal(CampusStatus.Active, (await Read(await client.PostAsync($"/api/v1/campuses/{second.Id}/activate", null))).Status);
        Assert.Equal(CampusStatus.Suspended, (await Read(await client.PostAsync($"/api/v1/campuses/{second.Id}/suspend", null))).Status);
        Assert.Equal(CampusStatus.Active, (await Read(await client.PostAsync($"/api/v1/campuses/{second.Id}/reactivate", null))).Status);
        Assert.Equal(CampusStatus.Archived, (await Read(await client.PostAsync($"/api/v1/campuses/{second.Id}/archive", null))).Status);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/schools/{school.Id}/campuses/by-code/main")).StatusCode);

        await using var otherFactory = new ApiFactory(database.ConnectionString, tenantId: otherTenant.Id); using var other = otherFactory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/campuses/{first.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync("/api/v1/campuses", new CreateCampusRequest(school.Id, "Invalid", "X"))).StatusCode);
    }

    private static async Task<CampusDto> Read(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("data").Deserialize<CampusDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
