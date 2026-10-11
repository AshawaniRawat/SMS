using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolERP.Application.Features.Schools;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SchoolApiTests(PostgresFixture database)
{
    [PostgresFact]
    public async Task SchoolApiIsTenantScopedAndSupportsProfileAndLifecycle()
    {
        var firstTenant = Tenant.Create("First API Tenant", "school-api-" + Guid.NewGuid().ToString("N"));
        var secondTenant = Tenant.Create("Second API Tenant", "school-api-" + Guid.NewGuid().ToString("N"));
        await using (var setup = database.CreateContext(platformAccess: true))
        {
            setup.Tenants.AddRange(firstTenant, secondTenant);
            await setup.SaveChangesAsync();
        }

        await using var firstFactory = new ApiFactory(database.ConnectionString, tenantId: firstTenant.Id);
        using var firstClient = firstFactory.CreateClient();
        var create = new CreateSchoolRequest(firstTenant.Id, "API School", " school-01 ",
            BoardType: SchoolBoardType.CBSE, Email: "SCHOOL@EXAMPLE.COM", CountryCode: "in", DefaultCurrency: "inr");
        var createdResponse = await firstClient.PostAsJsonAsync("/api/v1/schools", create);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var school = await Read(createdResponse);
        Assert.Equal("SCHOOL-01", school.Code);
        Assert.Equal(HttpStatusCode.Conflict, (await firstClient.PostAsJsonAsync("/api/v1/schools", create)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await firstClient.GetAsync($"/api/v1/schools/{school.Id}")).StatusCode);
        firstClient.DefaultRequestHeaders.Add("X-Tenant-Id", secondTenant.Id.ToString());
        Assert.Equal(HttpStatusCode.OK, (await firstClient.GetAsync($"/api/v1/schools/{school.Id}")).StatusCode);
        firstClient.DefaultRequestHeaders.Remove("X-Tenant-Id");
        Assert.Equal(HttpStatusCode.Forbidden, (await firstClient.PostAsJsonAsync("/api/v1/schools",
            create with { TenantId = secondTenant.Id, Code = "SPOOF" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await firstClient.GetAsync("/api/v1/schools/by-code/school-01")).StatusCode);

        var updated = new SchoolProfileRequest("Updated School", ShortName: "US", City: "Delhi", CountryCode: "IN");
        Assert.Equal(HttpStatusCode.OK, (await firstClient.PutAsJsonAsync($"/api/v1/schools/{school.Id}", updated)).StatusCode);
        Assert.Equal(SchoolStatus.Active, (await Post(firstClient, school.Id, "activate")).Status);
        Assert.Equal(SchoolStatus.Suspended, (await Post(firstClient, school.Id, "suspend")).Status);
        Assert.Equal(SchoolStatus.Active, (await Post(firstClient, school.Id, "reactivate")).Status);
        Assert.Equal(SchoolStatus.Archived, (await Post(firstClient, school.Id, "archive")).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await firstClient.PostAsync($"/api/v1/schools/{school.Id}/activate", null)).StatusCode);

        await using var secondFactory = new ApiFactory(database.ConnectionString, tenantId: secondTenant.Id);
        using var secondClient = secondFactory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/api/v1/schools/{school.Id}")).StatusCode);
        var secondCreate = create with { TenantId = secondTenant.Id };
        Assert.Equal(HttpStatusCode.Created, (await secondClient.PostAsJsonAsync("/api/v1/schools", secondCreate)).StatusCode);
    }

    private static async Task<SchoolDto> Post(HttpClient client, Guid id, string operation)
    {
        var response = await client.PostAsync($"/api/v1/schools/{id}/{operation}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await Read(response);
    }

    private static async Task<SchoolDto> Read(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Deserialize<SchoolDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
