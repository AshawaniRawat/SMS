using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class SchoolLifecycleTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime First = new(2026, 10, 10, 5, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreationNormalizesProfileAndRaisesEvent()
    {
        var school = School.Create(TenantId, "  Valley School ", " vs-01 ", SchoolBoardType.CBSE,
            " OFFICE@EXAMPLE.COM ", " 12345 ", "in", "Asia/Kolkata", "inr", First);

        Assert.Equal("Valley School", school.Name);
        Assert.Equal("VS-01", school.Code);
        Assert.Equal("office@example.com", school.Email);
        Assert.Equal("IN", school.CountryCode);
        Assert.Equal("INR", school.DefaultCurrency);
        Assert.Equal(SchoolStatus.Draft, school.Status);
        var created = Assert.IsType<SchoolCreatedDomainEvent>(Assert.Single(school.DomainEvents));
        Assert.Equal(TenantId, created.TenantId);
        Assert.Equal(First, created.OccurredOnUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-school")]
    [InlineData("school--one")]
    [InlineData("school one")]
    public void InvalidCodeIsRejected(string code) =>
        Assert.Throws<DomainException>(() => School.Create(TenantId, "Valid", code));

    [Fact]
    public void ProfileCanBeUpdatedWithoutChangingTenantOrCode()
    {
        var school = School.Create(TenantId, "Original", "S1");
        school.UpdateProfile("Updated", "UP", "REG", "AFF", "CBSE", SchoolBoardType.CBSE,
            "ADMIN@EXAMPLE.COM", "1", "2", "https://example.com", "Line 1", "Line 2",
            "Delhi", "Delhi", "110001", "in", "/logo.png", "Asia/Kolkata", "inr", "Principal");

        Assert.Equal("Updated", school.Name);
        Assert.Equal("S1", school.Code);
        Assert.Equal(TenantId, school.TenantId);
        Assert.Equal("admin@example.com", school.Email);
    }

    [Fact]
    public void DraftActiveSuspendedActiveArchivedLifecycleIsControlled()
    {
        var school = School.Create(TenantId, "Valid", "S1", occurredOnUtc: First);
        school.Activate(First.AddHours(1));
        school.Suspend(First.AddHours(2));
        school.Reactivate(First.AddHours(3));
        school.Archive(First.AddHours(4));

        Assert.Equal(SchoolStatus.Archived, school.Status);
        Assert.Equal(First.AddHours(1), school.ActivatedAt);
        Assert.Null(school.SuspendedAt);
        Assert.Equal(First.AddHours(4), school.ArchivedAt);
        Assert.Contains(school.DomainEvents, x => x is SchoolActivatedDomainEvent);
        Assert.Contains(school.DomainEvents, x => x is SchoolSuspendedDomainEvent);
        Assert.Contains(school.DomainEvents, x => x is SchoolReactivatedDomainEvent);
        Assert.Contains(school.DomainEvents, x => x is SchoolArchivedDomainEvent);
    }

    [Fact]
    public void InvalidTransitionsAndTenantReassignmentAreRejected()
    {
        var school = School.Create(TenantId, "Valid", "S1");
        Assert.Throws<InvalidSchoolStatusTransitionException>(() => school.Suspend(First));
        ((ITenantEntity)school).TenantId = TenantId;
        Assert.Throws<DomainException>(() => ((ITenantEntity)school).TenantId = Guid.NewGuid());
        school.Archive(First);
        Assert.Throws<InvalidSchoolStatusTransitionException>(() => school.Activate(First));
    }
}
