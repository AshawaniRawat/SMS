using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Infrastructure.Identity;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class ApplicationUserTests
{
    [Fact]
    public void New_user_is_pending_and_profile_is_trimmed()
    {
        var user = ApplicationUser.Create(" Ada ", " Lovelace ", " ADA@EXAMPLE.COM ");
        Assert.Equal(UserAccountStatus.Pending, user.Status);
        Assert.False(user.IsActive);
        Assert.Equal("Ada", user.FirstName);
        Assert.Equal("Lovelace", user.LastName);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Null(user.TenantId);
    }

    [Fact]
    public void User_can_activate_suspend_reactivate_and_deactivate()
    {
        var user = ApplicationUser.Create("Ada", "Lovelace", "ada@example.com");
        user.Activate(); Assert.Equal(UserAccountStatus.Active, user.Status); Assert.True(user.IsActive);
        user.Suspend(); Assert.Equal(UserAccountStatus.Suspended, user.Status); Assert.False(user.IsActive);
        user.Activate(); user.Deactivate(new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(UserAccountStatus.Deactivated, user.Status); Assert.NotNull(user.DeactivatedAt);
        Assert.Throws<InvalidUserAccountStatusTransitionException>(() => user.Activate());
    }

    [Fact]
    public void Profile_update_does_not_change_scope()
    {
        var tenantId = Guid.NewGuid(); var schoolId = Guid.NewGuid(); var campusId = Guid.NewGuid();
        var user = ApplicationUser.Create("Old", "Name", "user@example.com", tenantId, schoolId, campusId);
        user.UpdateProfile(" New ", " Name ", " Display ", "+123");
        Assert.Equal((tenantId, schoolId, campusId), (user.TenantId, user.SchoolId, user.CampusId));
        Assert.Equal("New", user.FirstName); Assert.Equal("Display", user.DisplayName);
    }

    [Fact]
    public void Invalid_scope_is_rejected()
    {
        Assert.Throws<DomainException>(() => ApplicationUser.Create("A", "B", "a@b.com", schoolId: Guid.NewGuid()));
        Assert.Throws<DomainException>(() => ApplicationUser.Create("A", "B", "a@b.com", Guid.NewGuid(), campusId: Guid.NewGuid()));
    }
}
