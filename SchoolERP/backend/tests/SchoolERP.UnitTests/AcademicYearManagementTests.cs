using SchoolERP.Application.Features.AcademicYears;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using Xunit;

namespace SchoolERP.UnitTests;
public sealed class AcademicYearManagementTests
{
    [Fact] public void Create_request_has_no_caller_controlled_ownership(){var p=typeof(CreateAcademicYearRequest).GetProperties().Select(x=>x.Name);Assert.DoesNotContain("TenantId",p);Assert.DoesNotContain("SchoolId",p);}
    [Fact] public void Valid_year_is_draft_and_code_is_normalized(){var y=AcademicYear.Create(Guid.NewGuid(),Guid.NewGuid()," 2026-27 "," ay-26 ",new(2026,4,1),new(2027,3,31));Assert.Equal(AcademicYearStatus.Draft,y.Status);Assert.Equal("AY-26",y.Code);Assert.False(y.IsCurrent);}
    [Theory][InlineData("2026-04-01","2026-04-01")][InlineData("2026-04-01","2028-04-02")] public void Invalid_date_ranges_are_rejected(string start,string end)=>Assert.Throws<DomainException>(()=>AcademicYear.ValidateDates(DateOnly.Parse(start),DateOnly.Parse(end)));
    [Theory][InlineData("2026-03-31")][InlineData("2027-04-01")] public void Lock_date_outside_year_is_rejected(string value){var y=AcademicYear.Create(Guid.NewGuid(),Guid.NewGuid(),"Year","Y",new(2026,4,1),new(2027,3,31));Assert.Throws<DomainException>(()=>y.SetLockDate(DateOnly.Parse(value)));}
    [Fact] public void Lock_date_inside_year_is_accepted(){var y=AcademicYear.Create(Guid.NewGuid(),Guid.NewGuid(),"Year","Y",new(2026,4,1),new(2027,3,31));y.SetLockDate(new(2027,1,1));Assert.Equal(new DateOnly(2027,1,1),y.LockDate);}
    [Fact] public void Admission_range_validator_rejects_reverse_range(){var v=new OpenAdmissionsRequestValidator();Assert.False(v.Validate(new OpenAdmissionsRequest(new(2026,5,2),new(2026,5,1))).IsValid);}
}
