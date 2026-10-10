using SchoolERP.Application.Features.Authentication;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class LoginValidationTests
{
    private readonly LoginRequestValidator _validator = new();

    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("user@example.com", "")]
    public void Invalid_login_requests_are_rejected(string email, string password) =>
        Assert.False(_validator.Validate(new LoginRequest(email, password)).IsValid);

    [Fact]
    public void Valid_login_shape_is_accepted() =>
        Assert.True(_validator.Validate(new LoginRequest("user@example.com", "any-existing-password")).IsValid);
}
