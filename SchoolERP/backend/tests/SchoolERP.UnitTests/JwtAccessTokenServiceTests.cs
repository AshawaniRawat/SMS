using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Security;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Identity;
using Xunit;
using SchoolERP.UnitTests.TestCommon;

namespace SchoolERP.UnitTests;

public sealed class JwtAccessTokenServiceTests
{
    private const string Key = "unit-test-signing-key-with-more-than-thirty-two-bytes";
    private static readonly DateTime IssuedAt = new(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Tenant_token_contains_standard_and_conditional_scope_claims()
    {
        var user = User(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var result = Service().GenerateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("SchoolERP.Tests", token.Issuer); Assert.Contains("SchoolERP.Client", token.Audiences);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.Equal(user.UserId.ToString(), token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.False(string.IsNullOrWhiteSpace(token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value));
        Assert.Equal(user.Email, token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.TenantId.ToString(), token.Claims.Single(x => x.Type == CustomClaimTypes.TenantId).Value);
        Assert.Equal(user.SchoolId.ToString(), token.Claims.Single(x => x.Type == CustomClaimTypes.SchoolId).Value);
        Assert.Equal(user.CampusId.ToString(), token.Claims.Single(x => x.Type == CustomClaimTypes.CampusId).Value);
        Assert.Equal(IssuedAt.AddMinutes(15), result.ExpiresAtUtc);
    }

    [Fact]
    public void Platform_token_omits_all_organizational_claims()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(Service().GenerateToken(User()).AccessToken);
        Assert.DoesNotContain(token.Claims, x => x.Type is CustomClaimTypes.TenantId or CustomClaimTypes.SchoolId or CustomClaimTypes.CampusId);
    }

    [Fact]
    public void Access_token_never_embeds_permission_claims()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(Service().GenerateToken(User(TestIds.TenantA,TestIds.SchoolA,TestIds.CampusA)).AccessToken);
        Assert.DoesNotContain(token.Claims,x=>x.Type.Contains("permission",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Correct_token_validates_and_wrong_key_issuer_audience_or_tampering_fails()
    {
        var raw = Service().GenerateToken(User()).AccessToken;
        Assert.NotNull(Validate(raw, Key, "SchoolERP.Tests", "SchoolERP.Client"));
        Assert.ThrowsAny<SecurityTokenException>(() => Validate(raw, new string('z', 48), "SchoolERP.Tests", "SchoolERP.Client"));
        Assert.ThrowsAny<SecurityTokenException>(() => Validate(raw, Key, "Wrong", "SchoolERP.Client"));
        Assert.ThrowsAny<SecurityTokenException>(() => Validate(raw, Key, "SchoolERP.Tests", "Wrong"));
        Assert.ThrowsAny<SecurityTokenException>(() => Validate(raw[..^1] + (raw[^1] == 'a' ? 'b' : 'a'), Key, "SchoolERP.Tests", "SchoolERP.Client"));
    }

    [Fact]
    public void Expired_token_is_rejected()
    {
        var raw = Service(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).GenerateToken(User()).AccessToken;
        Assert.Throws<SecurityTokenExpiredException>(() => Validate(raw, Key, "SchoolERP.Tests", "SchoolERP.Client", true));
    }

    private static ClaimsPrincipal Validate(string raw, string key, string issuer, string audience, bool validateLifetime = false) =>
        new JwtSecurityTokenHandler().ValidateToken(raw, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = audience,
            ValidateLifetime = validateLifetime, ValidateIssuerSigningKey = true, RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.Zero
        }, out _);
    private static JwtAccessTokenService Service(DateTime? issuedAt = null) => new(Options.Create(new JwtOptions
    {
        Issuer = "SchoolERP.Tests", Audience = "SchoolERP.Client", SigningKey = Key,
        AccessTokenLifetimeMinutes = 15, ClockSkewSeconds = 0
    }), new FixedTimeProvider(issuedAt ?? IssuedAt));
    private static AuthenticatedUserDto User(Guid? tenant = null, Guid? school = null, Guid? campus = null) =>
        new(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", null, tenant, school, campus,
            UserAccountStatus.Active, true, false, IssuedAt);
    private sealed class FixedTimeProvider(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc);
    }
}
