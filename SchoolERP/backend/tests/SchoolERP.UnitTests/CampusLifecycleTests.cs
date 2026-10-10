using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class CampusLifecycleTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact] public void CreationNormalizesFieldsAndRaisesEvent()
    {
        var campus = Campus.Create(TenantId, SchoolId, " Main Campus ", " main-01 ", true, "OFFICE@EXAMPLE.COM", "123", "in", "Asia/Kolkata", Now);
        Assert.Equal("Main Campus", campus.Name); Assert.Equal("MAIN-01", campus.Code); Assert.Equal("office@example.com", campus.Email);
        Assert.Equal("IN", campus.CountryCode); Assert.True(campus.IsMainCampus); Assert.Equal(CampusStatus.Draft, campus.Status);
        var created = Assert.IsType<CampusCreatedDomainEvent>(Assert.Single(campus.DomainEvents)); Assert.Equal(Now, created.OccurredOnUtc);
    }

    [Fact] public void RequiredOwnershipAndFieldsAreValidated()
    {
        Assert.Throws<DomainException>(() => Campus.Create(Guid.Empty, SchoolId, "Campus", "MAIN"));
        Assert.Throws<DomainException>(() => Campus.Create(TenantId, Guid.Empty, "Campus", "MAIN"));
        Assert.Throws<DomainException>(() => Campus.Create(TenantId, SchoolId, " ", "MAIN"));
        Assert.Throws<DomainException>(() => Campus.Create(TenantId, SchoolId, "Campus", "bad code"));
    }

    [Fact] public void ProfileAndCoordinatesAreControlled()
    {
        var campus = Campus.Create(TenantId, SchoolId, "Campus", "MAIN");
        campus.UpdateProfile("Updated", "UP", "ADMIN@EXAMPLE.COM", "1", "2", "A1", "A2", "Delhi", "Delhi", "110001", "in", "UTC", 28.6139m, 77.2090m);
        Assert.Equal("Updated", campus.Name); Assert.Equal("admin@example.com", campus.Email); Assert.Equal(28.6139m, campus.Latitude);
        Assert.Throws<DomainException>(() => campus.UpdateProfile("X", null, null, null, null, null, null, null, null, null, null, null, 91, null));
        Assert.Throws<DomainException>(() => campus.UpdateProfile("X", null, null, null, null, null, null, null, null, null, null, null, null, 181));
        Assert.Equal(TenantId, campus.TenantId); Assert.Equal(SchoolId, campus.SchoolId);
        Assert.Throws<DomainException>(() => ((ITenantEntity)campus).TenantId = Guid.NewGuid());
    }

    [Fact] public void LifecycleAndMainDesignationAreControlled()
    {
        var campus = Campus.Create(TenantId, SchoolId, "Campus", "MAIN");
        campus.MarkAsMainCampus(Now); Assert.True(campus.IsMainCampus); Assert.Contains(campus.DomainEvents, x => x is CampusMarkedAsMainDomainEvent);
        campus.Activate(Now); campus.Suspend(Now.AddHours(1)); campus.Reactivate(Now.AddHours(2)); campus.Archive(Now.AddHours(3));
        Assert.Equal(CampusStatus.Archived, campus.Status); Assert.Equal(Now, campus.ActivatedAt); Assert.Null(campus.SuspendedAt); Assert.Equal(Now.AddHours(3), campus.ArchivedAt);
        Assert.Throws<InvalidCampusStatusTransitionException>(() => campus.Activate(Now));
    }

    [Fact] public void InvalidLifecycleTransitionsAreRejected()
    {
        var draft = Campus.Create(TenantId, SchoolId, "Campus", "MAIN");
        Assert.Throws<InvalidCampusStatusTransitionException>(() => draft.Suspend(Now));
        var archived = Campus.Create(TenantId, SchoolId, "Other", "OTHER"); archived.Archive(Now);
        Assert.Throws<InvalidCampusStatusTransitionException>(() => archived.Reactivate(Now));
    }
}
