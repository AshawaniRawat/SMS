using SchoolERP.Application.Features.Authentication;
using SchoolERP.Infrastructure.Identity;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class PasswordRecoveryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Forgot_password_rejects_invalid_email(string email) =>
        Assert.False(new ForgotPasswordRequestValidator().Validate(new ForgotPasswordRequest(email)).IsValid);

    [Fact]
    public void Forgot_password_accepts_valid_email() =>
        Assert.True(new ForgotPasswordRequestValidator().Validate(new ForgotPasswordRequest("user@example.com")).IsValid);

    [Fact]
    public void Reset_validation_requires_all_fields_and_matching_passwords()
    {
        var validator = new ResetPasswordRequestValidator();
        Assert.False(validator.Validate(new ResetPasswordRequest("", "", "", "")).IsValid);
        Assert.False(validator.Validate(new ResetPasswordRequest("user@example.com", "token", "Valid#Pass1", "different")).IsValid);
        Assert.True(validator.Validate(new ResetPasswordRequest("user@example.com", "token", "Valid#Pass1", "Valid#Pass1")).IsValid);
    }

    [Fact]
    public void Identity_token_encoding_round_trips_url_sensitive_content_and_rejects_malformed_input()
    {
        var encoder = new IdentityTokenEncoder();
        const string raw = "Identity+/token== with unicode ✓";
        var encoded = encoder.Encode(raw);
        Assert.DoesNotContain('+', encoded); Assert.DoesNotContain('/', encoded); Assert.DoesNotContain('=', encoded);
        Assert.True(encoder.TryDecode(encoded, out var decoded)); Assert.Equal(raw, decoded);
        Assert.False(encoder.TryDecode("%%%not-base64%%%", out _));
    }
}
