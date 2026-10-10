using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SchoolERP.Application.Security;
using SchoolERP.Infrastructure.Identity;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class HttpCurrentUserTests
{
    [Fact]
    public void Reads_validated_identity_and_scope_claims_without_throwing_for_bad_values()
    {
        var userId = Guid.NewGuid(); var tenantId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(CustomClaimTypes.TenantId, tenantId.ToString()), new Claim(CustomClaimTypes.SchoolId, "invalid"),
            new Claim(ClaimTypes.Role, "FutureRole")], "Bearer"));
        var current = new HttpCurrentUser(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
        Assert.True(current.IsAuthenticated); Assert.Equal(userId, current.UserId); Assert.Equal(tenantId, current.TenantId);
        Assert.Null(current.SchoolId); Assert.Contains("FutureRole", current.Roles);
    }

    [Fact]
    public void Anonymous_context_is_empty()
    {
        var current = new HttpCurrentUser(new HttpContextAccessor());
        Assert.False(current.IsAuthenticated); Assert.Null(current.UserId); Assert.Null(current.TenantId);
    }
}
