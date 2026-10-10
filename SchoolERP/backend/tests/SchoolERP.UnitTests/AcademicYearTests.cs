using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class AcademicYearTests
{
    private static readonly Guid TenantId = Guid.NewGuid(); private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 4, 1); private static readonly DateOnly End = new(2027, 3, 31);
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact] public void CreationNormalizesAndStartsDraft()
    {
        var year = AcademicYear.Create(TenantId, SchoolId, " 2026 Session ", " ay2026-27 ", Start, End, " Setup ", Now);
        Assert.Equal("2026 Session", year.Name); Assert.Equal("AY2026-27", year.Code); Assert.Equal(AcademicYearStatus.Draft, year.Status);
        Assert.False(year.IsCurrent); Assert.False(year.IsAdmissionOpen); Assert.IsType<AcademicYearCreatedDomainEvent>(Assert.Single(year.DomainEvents));
    }

    [Fact] public void RequiredOwnershipNamesCodesAndDatesAreValidated()
    {
        Assert.Throws<DomainException>(() => AcademicYear.Create(Guid.Empty, SchoolId, "Year", "AY", Start, End));
        Assert.Throws<DomainException>(() => AcademicYear.Create(TenantId, Guid.Empty, "Year", "AY", Start, End));
        Assert.Throws<DomainException>(() => AcademicYear.Create(TenantId, SchoolId, " ", "AY", Start, End));
        Assert.Throws<DomainException>(() => AcademicYear.Create(TenantId, SchoolId, "Year", "bad code", Start, End));
        Assert.Throws<DomainException>(() => AcademicYear.Create(TenantId, SchoolId, "Year", "AY", Start, Start));
        Assert.Throws<DomainException>(() => AcademicYear.Create(TenantId, SchoolId, "Year", "AY", Start, Start.AddMonths(24).AddDays(1)));
    }

    [Fact] public void LifecycleIsControlledAndCurrentRequiresActive()
    {
        var year = New(); Assert.Throws<DomainException>(() => year.MarkAsCurrent(Now));
        year.Activate(Now); year.MarkAsCurrent(Now); Assert.True(year.IsCurrent);
        Assert.Throws<InvalidAcademicYearStatusTransitionException>(() => year.Archive(Now));
        year.Close(Now); Assert.False(year.IsCurrent); Assert.Equal(AcademicYearStatus.Closed, year.Status);
        Assert.Throws<InvalidAcademicYearStatusTransitionException>(() => year.Activate(Now));
        year.Archive(Now); Assert.Equal(AcademicYearStatus.Archived, year.Status);
        Assert.Throws<InvalidAcademicYearStatusTransitionException>(() => year.Activate(Now));
    }

    [Fact] public void DraftCanArchiveButCannotClose() { var year = New(); Assert.Throws<InvalidAcademicYearStatusTransitionException>(() => year.Close(Now)); year.Archive(Now); Assert.Equal(AcademicYearStatus.Archived, year.Status); }

    [Fact] public void AdmissionsAndLockDateAreValidated()
    {
        var year = New(); year.OpenAdmissions(Start.AddMonths(-2), Start, Now); Assert.True(year.IsAdmissionOpen);
        year.CloseAdmissions(Now); Assert.False(year.IsAdmissionOpen);
        Assert.Throws<DomainException>(() => year.OpenAdmissions(Start, Start.AddDays(-1), Now));
        year.SetLockDate(Start.AddMonths(6)); Assert.Equal(Start.AddMonths(6), year.LockDate); year.SetLockDate(null); Assert.Null(year.LockDate);
        Assert.Throws<DomainException>(() => year.SetLockDate(Start.AddDays(-1))); Assert.Throws<DomainException>(() => year.SetLockDate(End.AddDays(1)));
        year.Archive(Now); Assert.Throws<DomainException>(() => year.OpenAdmissions(null, null, Now));
    }

    [Fact] public void OwnershipCannotChange() { var year = New(); Assert.Throws<DomainException>(() => ((ITenantEntity)year).TenantId = Guid.NewGuid()); Assert.Equal(SchoolId, year.SchoolId); }
    private static AcademicYear New() => AcademicYear.Create(TenantId, SchoolId, "2026-27", "AY2026", Start, End);
}
