using SchoolERP.Application.Features.Campuses;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Exceptions;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class CampusManagementTests
{
    [Fact]
    public void Create_request_does_not_expose_tenant_or_school_ownership()
    {
        var properties = typeof(CreateCampusRequest).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("TenantId", properties);
        Assert.DoesNotContain("SchoolId", properties);
    }

    [Theory]
    [InlineData("bad code")]
    [InlineData("CODE!")]
    [InlineData("")]
    public void Invalid_code_is_rejected(string code)
    {
        var validator = new CreateCampusRequestValidator();
        Assert.False(validator.Validate(new CreateCampusRequest("Campus", code)).IsValid);
    }

    [Fact]
    public void Coordinates_require_both_values()
    {
        var validator = new CampusProfileRequestValidator();
        var result = validator.Validate(new CampusProfileRequest("Campus", Latitude: 12.34m));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(-91, 10)]
    [InlineData(91, 10)]
    [InlineData(10, -181)]
    [InlineData(10, 181)]
    public void Coordinates_outside_geographic_bounds_are_rejected(double latitude, double longitude)
    {
        var validator = new CampusProfileRequestValidator();
        var result = validator.Validate(new CampusProfileRequest("Campus", Latitude: (decimal)latitude, Longitude: (decimal)longitude));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Campus_domain_normalizes_code_and_country()
    {
        var campus = Campus.Create(Guid.NewGuid(), Guid.NewGuid(), " Main ", " main-01 ", countryCode: " in ");
        Assert.Equal("MAIN-01", campus.Code);
        Assert.Equal("IN", campus.CountryCode);
        Assert.Equal("Main", campus.Name);
    }

    [Fact]
    public void Campus_domain_rejects_partial_coordinates()
    {
        var campus = Campus.Create(Guid.NewGuid(), Guid.NewGuid(), "Main", "MAIN");
        Assert.Throws<DomainException>(() => campus.UpdateProfile("Main", null, null, null, null,
            null, null, null, null, null, null, null, 10m, null));
    }
}
