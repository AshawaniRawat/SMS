using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Security;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class ResourceScopeValidatorTests
{
    [Fact]
    public void Tenant_scoped_user_can_access_any_school_and_campus()
    {
        var validator = new ResourceScopeValidator(new User(null, null));
        Assert.True(validator.CanAccessSchool(Guid.NewGuid()));
        Assert.True(validator.CanAccessCampus(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void School_scoped_user_is_limited_to_its_school()
    {
        var school = Guid.NewGuid();
        var validator = new ResourceScopeValidator(new User(school, null));
        Assert.True(validator.CanAccessSchool(school));
        Assert.False(validator.CanAccessSchool(Guid.NewGuid()));
    }

    [Fact]
    public void Campus_scoped_user_is_limited_to_its_school_and_campus()
    {
        var school = Guid.NewGuid(); var campus = Guid.NewGuid();
        var validator = new ResourceScopeValidator(new User(school, campus));
        Assert.True(validator.CanAccessCampus(school, campus));
        Assert.False(validator.CanAccessCampus(school, Guid.NewGuid()));
        Assert.False(validator.CanAccessCampus(Guid.NewGuid(), campus));
    }

    private sealed record User(Guid? SchoolId, Guid? CampusId) : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public Guid? TenantId => Guid.NewGuid();
        public string? Email => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => [];
    }
}
