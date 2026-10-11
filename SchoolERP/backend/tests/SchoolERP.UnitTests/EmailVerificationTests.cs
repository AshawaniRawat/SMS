using SchoolERP.Application.Features.Authentication;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class EmailVerificationTests
{
    [Fact]
    public void Confirm_request_requires_user_and_token()
    {
        var validator = new ConfirmEmailRequestValidator();
        Assert.False(validator.Validate(new ConfirmEmailRequest(Guid.Empty, "token")).IsValid);
        Assert.False(validator.Validate(new ConfirmEmailRequest(Guid.NewGuid(), "")).IsValid);
        Assert.True(validator.Validate(new ConfirmEmailRequest(Guid.NewGuid(), "encoded-token")).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void Resend_rejects_invalid_email(string email) => Assert.False(
        new ResendEmailVerificationRequestValidator().Validate(new ResendEmailVerificationRequest(email)).IsValid);

    [Fact]
    public void Resend_accepts_valid_email() => Assert.True(
        new ResendEmailVerificationRequestValidator().Validate(
            new ResendEmailVerificationRequest("user@example.com")).IsValid);
}
