using SchoolERP.Application.Features.Authentication;
namespace SchoolERP.Application.Security;
public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");
public sealed record CurrentUserDto(Guid? UserId, string? Email, Guid? TenantId, Guid? SchoolId, Guid? CampusId,
    IReadOnlyCollection<string> Roles);
public interface IAccessTokenService
{
    AccessTokenResult GenerateToken(AuthenticatedUserDto user, IReadOnlyCollection<string>? roles = null);
}
