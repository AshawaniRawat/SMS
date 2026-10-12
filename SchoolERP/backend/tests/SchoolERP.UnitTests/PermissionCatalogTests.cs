using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Security;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void Catalog_is_valid_deterministic_and_contains_sensitive_permissions()
    {
        Assert.Empty(PermissionCatalog.Validate());
        Assert.Equal(168, PermissionCatalog.All.Count);
        Assert.Equal(PermissionCatalog.All.Count,
            PermissionCatalog.All.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(PermissionCatalog.All, item =>
        {
            Assert.Equal($"{item.Module}.{item.Action}", item.Code);
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.False(string.IsNullOrWhiteSpace(item.Name));
            Assert.True(item.SortOrder >= 0);
        });
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Fee.Refund));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Exam.Publish));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Payroll.Approve));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.User.AssignRole));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Class.Archive));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Section.Archive));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Subject.ManageClassMapping));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Department.Archive));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.House.Archive));
        Assert.NotNull(PermissionCatalog.FindByCode(PermissionCatalog.Calendar.Publish));
    }

    [Fact]
    public void Entity_preserves_code_and_allows_controlled_metadata_sync()
    {
        var source = PermissionCatalog.FindByCode(PermissionCatalog.Student.View)!;
        var permission = Permission.CreateFromDefinition(source);
        var changed = source with { Name = "View learner records", Description = "Updated metadata", IsActive = false };

        permission.Synchronize(changed);

        Assert.Equal(PermissionCatalog.Student.View, permission.Code);
        Assert.Equal("View learner records", permission.Name);
        Assert.False(permission.IsActive);
        Assert.Throws<DomainException>(() => permission.Synchronize(changed with { Code = "Student.Edit", Action = "Edit" }));
    }

    [Theory]
    [InlineData("student.View")]
    [InlineData("Student.view")]
    [InlineData("Student.View.More")]
    [InlineData("Student_View")]
    public void Invalid_codes_are_rejected(string code)
    {
        var definition = new PermissionDefinition(Guid.NewGuid(), code, "Test", "Student", "View", null,
            PermissionScope.School, 10);
        Assert.Throws<DomainException>(() => Permission.CreateFromDefinition(definition));
    }
}
