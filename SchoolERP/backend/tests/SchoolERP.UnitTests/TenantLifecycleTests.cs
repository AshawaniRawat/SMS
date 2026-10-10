using SchoolERP.Domain.Constants;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class TenantLifecycleTests
{
    private static readonly DateTime First = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Second = First.AddDays(1);

    [Fact]
    public void CreationNormalizesFieldsAndRaisesEvent()
    {
        var tenant = Tenant.Create("  ABC Education  ", " ABC-SCHOOL ", " ADMIN@EXAMPLE.COM ", " +1 555 ", "us", "UTC", First);
        tenant.UpdateProfile(tenant.Name, " ABC Education LLC ", tenant.Email, tenant.Phone, " https://example.com ",
            " https://example.com/logo.png ", tenant.CountryCode, tenant.TimeZoneId, "usd");

        Assert.Equal("ABC Education", tenant.Name);
        Assert.Equal("abc-school", tenant.Slug);
        Assert.Equal("admin@example.com", tenant.Email);
        Assert.Equal("US", tenant.CountryCode);
        Assert.Equal("USD", tenant.DefaultCurrency);
        Assert.Equal(TenantStatus.Pending, tenant.Status);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        var created = Assert.IsType<TenantCreatedDomainEvent>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(First, created.OccurredOnUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc_school")]
    [InlineData("-school")]
    [InlineData("school-")]
    [InlineData("school--group")]
    public void InvalidSlugIsRejected(string slug) =>
        Assert.Throws<DomainException>(() => Tenant.Create("Valid", slug));

    [Fact]
    public void EveryReservedSlugIsRejected()
    {
        foreach (var slug in ReservedTenantSlugs.All)
            Assert.Throws<DomainException>(() => Tenant.Create("Valid", slug));
    }

    [Fact]
    public void EmptyAndOverlongNamesAreRejected()
    {
        Assert.Throws<DomainException>(() => Tenant.Create("   ", "valid"));
        Assert.Throws<DomainException>(() => Tenant.Create(new string('a', 201), "valid"));
    }

    [Fact]
    public void PendingActiveSuspendedActiveArchivedLifecycleIsControlled()
    {
        var tenant = Tenant.Create("Valid", "valid", occurredOnUtc: First);
        tenant.Activate(First);
        Assert.Equal(TenantStatus.Active, tenant.Status); Assert.Equal(First, tenant.ActivatedAt);
        tenant.Suspend(Second);
        Assert.Equal(TenantStatus.Suspended, tenant.Status); Assert.Equal(Second, tenant.SuspendedAt);
        tenant.Reactivate(Second.AddHours(1));
        Assert.Equal(TenantStatus.Active, tenant.Status); Assert.Null(tenant.SuspendedAt); Assert.Equal(First, tenant.ActivatedAt);
        tenant.Archive(Second.AddHours(2));
        Assert.Equal(TenantStatus.Archived, tenant.Status); Assert.Equal(Second.AddHours(2), tenant.ArchivedAt);
        Assert.Contains(tenant.DomainEvents, item => item is TenantActivatedDomainEvent);
        Assert.Contains(tenant.DomainEvents, item => item is TenantSuspendedDomainEvent);
        Assert.Contains(tenant.DomainEvents, item => item is TenantReactivatedDomainEvent);
        Assert.Contains(tenant.DomainEvents, item => item is TenantArchivedDomainEvent);
    }

    [Fact]
    public void PendingAndSuspendedMayArchive()
    {
        var pending = Tenant.Create("Pending", "pending"); pending.Archive(First);
        var suspended = Tenant.Create("Suspended", "suspended"); suspended.Activate(First); suspended.Suspend(Second); suspended.Archive(Second);
        Assert.Equal(TenantStatus.Archived, pending.Status); Assert.Equal(TenantStatus.Archived, suspended.Status);
    }

    [Fact]
    public void InvalidTransitionsAndArchivedReactivationAreRejected()
    {
        var pending = Tenant.Create("Pending", "pending-two");
        Assert.Throws<InvalidTenantStatusTransitionException>(() => pending.Suspend(First));
        var archived = Tenant.Create("Archived", "archived"); archived.Archive(First);
        Assert.Throws<InvalidTenantStatusTransitionException>(() => archived.Activate(Second));
        Assert.Throws<InvalidTenantStatusTransitionException>(() => archived.Reactivate(Second));
    }

    [Fact]
    public void TenantIsPlatformScopedAndNotSoftDeletable()
    {
        var tenant = Tenant.Create("Platform", "platform-tenant");
        Assert.False((object)tenant is ITenantEntity);
        Assert.False((object)tenant is ISoftDeletableEntity);
    }
}
