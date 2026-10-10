using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolERP.Application.Features.AcademicYears;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AcademicYearApiTests(PostgresFixture database)
{
    [PostgresFact] public async Task ApiEnforcesOverlapCurrentSwitchLifecycleAndTenantIsolation()
    {
        var tenant = Tenant.Create("Academic API", "academic-api-" + Guid.NewGuid().ToString("N")); tenant.Activate(DateTime.UtcNow);
        var school = School.Create(tenant, "Academic School", "AS"); school.Activate(DateTime.UtcNow);
        var other = Tenant.Create("Other Academic", "academic-other-" + Guid.NewGuid().ToString("N")); other.Activate(DateTime.UtcNow);
        await using (var setup = database.CreateContext(platformAccess: true)) { setup.Schools.Add(school); setup.Tenants.Add(other); await setup.SaveChangesAsync(); }

        await using var factory = new ApiFactory(database.ConnectionString, tenantId: tenant.Id); using var client = factory.CreateClient();
        var firstRequest = new CreateAcademicYearRequest(school.Id, "2026-27", "ay2026", new(2026, 4, 1), new(2027, 3, 31));
        var firstResponse = await client.PostAsJsonAsync("/api/v1/academic-years", firstRequest); Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode); var first = await Read(firstResponse);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/academic-years", firstRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/academic-years", firstRequest with { Code = "OVERLAP", StartDate = new(2026, 6, 1), EndDate = new(2027, 5, 31) })).StatusCode);
        var second = await Read(await client.PostAsJsonAsync("/api/v1/academic-years", firstRequest with { Name = "2027-28", Code = "AY2027", StartDate = new(2027, 4, 1), EndDate = new(2028, 3, 31) }));
        first = await Read(await client.PostAsync($"/api/v1/academic-years/{first.Id}/activate", null));
        first = await Read(await client.PostAsync($"/api/v1/academic-years/{first.Id}/set-current", null)); Assert.True(first.IsCurrent);
        second = await Read(await client.PostAsync($"/api/v1/academic-years/{second.Id}/activate", null));
        second = await Read(await client.PostAsync($"/api/v1/academic-years/{second.Id}/set-current", null)); Assert.True(second.IsCurrent);
        var rereadFirst = await Read(await client.GetAsync($"/api/v1/academic-years/{first.Id}")); Assert.False(rereadFirst.IsCurrent);
        second = await Read(await client.PostAsJsonAsync($"/api/v1/academic-years/{second.Id}/admissions/open", new OpenAdmissionsRequest(new(2027, 1, 1), new(2027, 3, 1)))); Assert.True(second.IsAdmissionOpen);
        second = await Read(await client.PostAsync($"/api/v1/academic-years/{second.Id}/close", null)); Assert.False(second.IsCurrent); Assert.False(second.IsAdmissionOpen);
        second = await Read(await client.PostAsync($"/api/v1/academic-years/{second.Id}/archive", null)); Assert.Equal(AcademicYearStatus.Archived, second.Status);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/schools/{school.Id}/academic-years/by-code/ay2026")).StatusCode);

        await using var otherFactory = new ApiFactory(database.ConnectionString, tenantId: other.Id); using var otherClient = otherFactory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync($"/api/v1/academic-years/{first.Id}")).StatusCode);
    }

    private static async Task<AcademicYearDto> Read(HttpResponseMessage response)
    { Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync()); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.GetProperty("data").Deserialize<AcademicYearDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!; }
}
