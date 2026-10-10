using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Exceptions;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class TenantTests
{
    [Theory]
    [InlineData("school")]
    [InlineData("north-school-123")]
    [InlineData("123")]
    public void SlugAcceptsLowercaseUrlSegments(string slug) => Assert.Equal(slug, Tenant.Create("Test", slug).Slug);

    [Theory]
    [InlineData("")]
    [InlineData("not friendly")]
    [InlineData("-school")]
    [InlineData("school-")]
    [InlineData("school--north")]
    [InlineData("school/north")]
    public void SlugRejectsInvalidValues(string slug) => Assert.Throws<DomainException>(() => Tenant.Create("Test", slug));

    [Fact] public void SlugRejectsMoreThan100Characters() => Assert.Throws<DomainException>(() => Tenant.Create("Test", new string('a', 101)));

    [Fact] public void SlugIsTrimmedAndNormalizedInvariantly() => Assert.Equal("north-school", Tenant.Create("Test", " NORTH-SCHOOL ").Slug);
}
