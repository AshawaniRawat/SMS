using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SchoolERP.Application.Security;

namespace SchoolERP.IntegrationTests;

public sealed record TestUserSpec(Guid UserId, string Email, Guid? TenantId = null,
    Guid? SchoolId = null, Guid? CampusId = null);

public static class TestJwtTokenFactory
{
    public const string Issuer = "SchoolERP.Tests";
    public const string Audience = "SchoolERP.Tests.Client";
    public const string SigningKey = "test-only-signing-key-that-is-at-least-32-bytes-long";

    public static string Create(TestUserSpec user, DateTime? expiresUtc = null, string? signingKey = null)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (user.TenantId is { } tenantId) claims.Add(new(CustomClaimTypes.TenantId, tenantId.ToString()));
        if (user.SchoolId is { } schoolId) claims.Add(new(CustomClaimTypes.SchoolId, schoolId.ToString()));
        if (user.CampusId is { } campusId) claims.Add(new(CustomClaimTypes.CampusId, campusId.ToString()));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var expires = expiresUtc ?? now.AddMinutes(15);
        var notBefore = expires <= now ? expires.AddMinutes(-5) : now.AddMinutes(-1);
        var token = new JwtSecurityToken(Issuer, Audience, claims, notBefore, expires, credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
