using SchoolERP.Application.Features.Identity;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class UserRegistrationValidationTests
{
    private readonly CreatePlatformUserRequestValidator _platform = new();
    private readonly CreateTenantUserRequestValidator _tenant = new();

    [Theory]
    [InlineData("", "Last", "valid@example.com", "password")]
    [InlineData("First", "", "valid@example.com", "password")]
    [InlineData("First", "Last", "", "password")]
    [InlineData("First", "Last", "not-an-email", "password")]
    [InlineData("First", "Last", "valid@example.com", "")]
    public void Platform_request_rejects_missing_or_invalid_required_values(string first, string last, string email, string password) =>
        Assert.False(_platform.Validate(new CreatePlatformUserRequest(first, last, email, password)).IsValid);

    [Fact]
    public void Display_name_and_phone_lengths_are_enforced() =>
        Assert.False(_platform.Validate(new CreatePlatformUserRequest("First", "Last", "valid@example.com", "password", new string('x', 201), new string('1', 31))).IsValid);

    [Fact]
    public void Tenant_request_requires_tenant_and_school_for_campus()
    {
        Assert.False(_tenant.Validate(new CreateTenantUserRequest(Guid.Empty, "First", "Last", "valid@example.com", "password")).IsValid);
        Assert.False(_tenant.Validate(new CreateTenantUserRequest(Guid.NewGuid(), "First", "Last", "valid@example.com", "password", CampusId: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Valid_platform_and_tenant_scope_shapes_pass_request_validation()
    {
        Assert.True(_platform.Validate(new CreatePlatformUserRequest("First", "Last", "valid@example.com", "password")).IsValid);
        var tenantId = Guid.NewGuid(); var schoolId = Guid.NewGuid();
        Assert.True(_tenant.Validate(new CreateTenantUserRequest(tenantId, "First", "Last", "valid@example.com", "password")).IsValid);
        Assert.True(_tenant.Validate(new CreateTenantUserRequest(tenantId, "First", "Last", "valid@example.com", "password", schoolId)).IsValid);
        Assert.True(_tenant.Validate(new CreateTenantUserRequest(tenantId, "First", "Last", "valid@example.com", "password", schoolId, Guid.NewGuid())).IsValid);
    }
}
