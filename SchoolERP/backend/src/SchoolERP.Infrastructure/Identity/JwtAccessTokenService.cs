using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Application.Security;
namespace SchoolERP.Infrastructure.Identity;
public sealed class JwtAccessTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenService
{
    public AccessTokenResult GenerateToken(AuthenticatedUserDto user, IReadOnlyCollection<string>? roles = null)
    {
        var settings = options.Value; var issuedAt = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = issuedAt.AddMinutes(settings.AccessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Email, user.Email), new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName), new(JwtRegisteredClaimNames.FamilyName, user.LastName)
        };
        if (user.TenantId.HasValue) claims.Add(new(CustomClaimTypes.TenantId, user.TenantId.Value.ToString()));
        if (user.SchoolId.HasValue) claims.Add(new(CustomClaimTypes.SchoolId, user.SchoolId.Value.ToString()));
        if (user.CampusId.HasValue) claims.Add(new(CustomClaimTypes.CampusId, user.CampusId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(user.DisplayName)) claims.Add(new(ClaimTypes.Name, user.DisplayName));
        foreach (var role in roles?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal) ?? []) claims.Add(new(ClaimTypes.Role, role));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, issuedAt, expiresAt, credentials);
        return new(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
