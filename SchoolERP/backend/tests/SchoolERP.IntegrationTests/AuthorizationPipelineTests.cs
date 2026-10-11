using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Api.Security;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Security;
using Xunit;
using SchoolERP.Api.Controllers.v1;
using SchoolERP.Domain.Security;

namespace SchoolERP.IntegrationTests;

public sealed class AuthorizationPipelineTests
{
    [Fact]
    public void Platform_dashboard_declares_platform_permission()
    {
        var method = typeof(PlatformDashboardController).GetMethod(nameof(PlatformDashboardController.Get));
        var attribute = Assert.Single(method!.GetCustomAttributes(typeof(RequirePermissionAttribute), true)
            .Cast<RequirePermissionAttribute>());
        Assert.Equal($"Permission:{PermissionCatalog.Platform.ViewDashboard}", attribute.Policy);
    }

    [Fact]
    public async Task Dynamic_provider_builds_permission_policy_and_handles_other_and_malformed_names()
    {
        var provider = new PermissionAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
        var policy = await provider.GetPolicyAsync("Permission:Student.View");
        var requirement = Assert.Single(policy!.Requirements.OfType<PermissionRequirement>());
        Assert.Equal("Student.View", requirement.PermissionCode);
        Assert.Null(await provider.GetPolicyAsync("Permission:"));
        Assert.Null(await provider.GetPolicyAsync("unregistered-policy"));
    }

    [Fact]
    public void Attribute_builds_canonical_policy_and_requirement_rejects_empty_code()
    {
        Assert.Equal("Permission:Student.View", new RequirePermissionAttribute("Student.View").Policy);
        Assert.Throws<ArgumentException>(() => new RequirePermissionAttribute(""));
        Assert.Throws<ArgumentException>(() => new PermissionRequirement(" "));
    }

    [Theory]
    [InlineData(true, "Student.View", "Student.View", true)]
    [InlineData(true, "Student.View", "Fee.View", false)]
    [InlineData(true, "Student.View", "student.view", false)]
    [InlineData(false, "Student.View", "Student.View", false)]
    public async Task Handler_requires_authentication_and_matches_codes_ordinally(bool authenticated,
        string granted, string required, bool expected)
    {
        var userId = Guid.NewGuid();
        var current = new FakeCurrentUser(authenticated, authenticated ? userId : null);
        var permissions = new FakePermissions([granted]);
        var http = new DefaultHttpContext();
        var handler = new PermissionAuthorizationHandler(permissions, current,
            new HttpContextAccessor { HttpContext = http }, NullLogger<PermissionAuthorizationHandler>.Instance);
        var requirement = new PermissionRequirement(required);
        var identity = new ClaimsIdentity(authenticated ? "test" : null);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), http);
        await handler.HandleAsync(context);
        Assert.Equal(expected, context.HasSucceeded);
    }

    private sealed record FakeCurrentUser(bool IsAuthenticated, Guid? UserId) : ICurrentUser
    {
        public Guid? TenantId => Guid.NewGuid(); public Guid? SchoolId => null; public Guid? CampusId => null;
        public string? Email => null; public IReadOnlyCollection<string> Roles => [];
    }
    private sealed class FakePermissions(IEnumerable<string> codes) : IUserPermissionService
    {
        private readonly IReadOnlySet<string> _codes = new HashSet<string>(codes, StringComparer.Ordinal);
        public Task<IReadOnlySet<string>> GetEffectivePermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(_codes);
        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default) => Task.FromResult(_codes.Contains(permissionCode));
    }
}
