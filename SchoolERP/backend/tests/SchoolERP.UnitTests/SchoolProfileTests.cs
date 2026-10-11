using SchoolERP.Application.Features.SchoolProfile;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Events;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class SchoolProfileTests
{
    private readonly UpdateSchoolProfileRequestValidator _validator = new();

    [Fact]
    public void Valid_update_normalizes_profile_values_and_records_event()
    {
        var school = School.Create(Guid.NewGuid(), "Original", "SCH");
        var occurredAt = new DateTime(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc);

        school.UpdateProfile("  New School  ", "  NS  ", null, null, "  BOARD  ", null,
            "  OFFICE@EXAMPLE.COM ", null, null, " https://example.com ", null, null,
            null, null, null, " in ", null, " Asia/Kolkata ", " inr ", " Principal ", occurredAt);

        Assert.Equal("New School", school.Name);
        Assert.Equal("office@example.com", school.Email);
        Assert.Equal("IN", school.CountryCode);
        Assert.Equal("INR", school.DefaultCurrency);
        Assert.Contains(school.DomainEvents, x => x is SchoolProfileUpdatedDomainEvent profile && profile.OccurredAtUtc == occurredAt);
    }

    [Fact]
    public void Empty_name_is_rejected()
    {
        var result = _validator.Validate(ValidRequest() with { Name = " " });
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateSchoolProfileRequest.Name));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("not-a-url")]
    [InlineData("file:///secret.txt")]
    public void Unsafe_or_malformed_website_is_rejected(string website)
    {
        var result = _validator.Validate(ValidRequest() with { Website = website });
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateSchoolProfileRequest.Website));
    }

    [Theory]
    [InlineData("I")]
    [InlineData("123")]
    public void Invalid_country_code_is_rejected(string countryCode)
    {
        var result = _validator.Validate(ValidRequest() with { CountryCode = countryCode });
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateSchoolProfileRequest.CountryCode));
    }

    private static UpdateSchoolProfileRequest ValidRequest() => new("School", Website: "https://example.com", CountryCode: "in", DefaultCurrency: "inr");
}
