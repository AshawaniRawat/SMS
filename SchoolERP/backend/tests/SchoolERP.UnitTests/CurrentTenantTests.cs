using Microsoft.Extensions.Logging.Abstractions;
using SchoolERP.Application.Abstractions;
using SchoolERP.Infrastructure.Tenancy;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class CurrentTenantTests
{
    [Fact]
    public void Authenticated_tenant_claim_is_authoritative()
    {
        var id = Guid.NewGuid();
        var tenant = Create(new User(true, id, true, false));
        Assert.True(tenant.IsAvailable); Assert.Equal(id, tenant.TenantId); Assert.False(tenant.IsPlatformScope);
    }

    [Fact]
    public void Missing_or_malformed_claim_is_safe_and_does_not_enable_platform_scope()
    {
        Assert.False(Create(new User(true, null, false, false)).IsAvailable);
        Assert.False(Create(new User(true, null, true, true)).IsAvailable);
        Assert.False(Create(new User(false, Guid.NewGuid(), true, false)).IsAvailable);
        Assert.False(Create(new User(true, null, false, false)).IsPlatformScope);
    }

    [Fact]
    public void Platform_scope_requires_explicit_privileged_accessor()
    {
        var tenant = new CurrentTenant(new User(true, null, false, false), new Platform(true),
            NullLogger<CurrentTenant>.Instance);
        Assert.False(tenant.IsAvailable); Assert.True(tenant.IsPlatformScope);
    }

    private static CurrentTenant Create(ICurrentUser user) => new(user, new Platform(false), NullLogger<CurrentTenant>.Instance);
    private sealed record Platform(bool IsEnabled) : IPlatformDataAccess;
    private sealed record User(bool IsAuthenticated, Guid? UserIdValue, bool HasTenantClaim,
        bool HasMalformedTenantClaim) : ICurrentUser
    {
        public Guid? UserId => UserIdValue; public Guid? TenantId => HasMalformedTenantClaim ? null : UserIdValue;
        public Guid? SchoolId => null; public Guid? CampusId => null; public string? Email => null;
        public IReadOnlyCollection<string> Roles => [];
    }
}
