using SchoolERP.Application.Features.SchoolClasses;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class SchoolClassManagementTests
{
    [Fact]
    public void Create_request_has_no_caller_controlled_tenant_or_school()
    {
        var properties = typeof(CreateSchoolClassRequest).GetProperties().Select(x => x.Name);
        Assert.DoesNotContain("TenantId", properties);
        Assert.DoesNotContain("SchoolId", properties);
    }

    [Fact]
    public void Valid_class_is_draft_preserves_ownership_and_normalizes_code()
    {
        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var yearId = Guid.NewGuid();

        var schoolClass = SchoolClass.Create(tenantId, schoolId, yearId, " Grade 1 ", " gr-1 ", 0);

        Assert.Equal(tenantId, schoolClass.TenantId);
        Assert.Equal(schoolId, schoolClass.SchoolId);
        Assert.Equal(yearId, schoolClass.AcademicYearId);
        Assert.Equal("Grade 1", schoolClass.Name);
        Assert.Equal("GR-1", schoolClass.Code);
        Assert.Equal(SchoolClassStatus.Draft, schoolClass.Status);
    }

    [Theory]
    [InlineData("grade 1")]
    [InlineData("GRADE@1")]
    [InlineData("")]
    public void Invalid_codes_are_rejected(string code) =>
        Assert.Throws<DomainException>(() => SchoolClass.NormalizeCode(code));

    [Fact]
    public void Negative_display_order_is_rejected_but_zero_is_allowed()
    {
        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        Assert.Equal(0, SchoolClass.Create(tenantId, schoolId, yearId, "Grade 1", "G1", 0).DisplayOrder);
        Assert.Throws<DomainException>(() => SchoolClass.Create(tenantId, schoolId, yearId, "Grade 2", "G2", -1));
    }

    [Fact]
    public void Archived_class_is_read_only()
    {
        var schoolClass = SchoolClass.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Grade 1", "G1", 0);
        schoolClass.Archive(DateTime.UtcNow);
        Assert.Throws<DomainException>(() => schoolClass.UpdateDetails("Changed", null, 0, null, null, DateTime.UtcNow));
    }
}
