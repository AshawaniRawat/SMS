using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Security;
namespace SchoolERP.Infrastructure.Identity;
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid? UserId => GuidClaim(ClaimTypes.NameIdentifier);
    public Guid? TenantId => GuidClaim(CustomClaimTypes.TenantId);
    public Guid? SchoolId => GuidClaim(CustomClaimTypes.SchoolId);
    public Guid? CampusId => GuidClaim(CustomClaimTypes.CampusId);
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");
    public IReadOnlyCollection<string> Roles => Principal?.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray() ?? [];
    public bool HasTenantClaim => Principal?.HasClaim(x => x.Type == CustomClaimTypes.TenantId) == true;
    public bool HasMalformedTenantClaim => HasTenantClaim && (!TenantId.HasValue || TenantId.Value == Guid.Empty);
    private Guid? GuidClaim(string type) => Guid.TryParse(Principal?.FindFirstValue(type), out var value) ? value : null;
}
