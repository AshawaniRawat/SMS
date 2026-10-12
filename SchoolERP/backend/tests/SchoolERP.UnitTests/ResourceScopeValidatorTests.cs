using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Security;
using Xunit;
using SchoolERP.UnitTests.TestCommon;

namespace SchoolERP.UnitTests;

public sealed class ResourceScopeValidatorTests
{
    [Fact]
    public void Tenant_scoped_user_can_access_any_school_and_campus()
    {
        var validator = new ResourceScopeValidator(new User(null, null));
        Assert.True(validator.CanAccessSchool(TestIds.SchoolA));
        Assert.True(validator.CanAccessCampus(TestIds.SchoolA, TestIds.CampusA));
    }

    [Fact]
    public void School_scoped_user_is_limited_to_its_school()
    {
        var school = TestIds.SchoolA;
        var validator = new ResourceScopeValidator(new User(school, null));
        Assert.True(validator.CanAccessSchool(school));
        Assert.False(validator.CanAccessSchool(TestIds.SchoolB));
    }

    [Fact]
    public void Campus_scoped_user_is_limited_to_its_school_and_campus()
    {
        var school = TestIds.SchoolA; var campus = TestIds.CampusA;
        var validator = new ResourceScopeValidator(new User(school, campus));
        Assert.True(validator.CanAccessCampus(school, campus));
        Assert.False(validator.CanAccessCampus(school, TestIds.CampusB));
        Assert.False(validator.CanAccessCampus(TestIds.SchoolB, campus));
    }

    private sealed record User(Guid? SchoolId, Guid? CampusId) : ICurrentUser
    {
        public Guid? UserId => TestIds.UserA;
        public Guid? TenantId => TestIds.TenantA;
        public string? Email => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => [];
    }
}
