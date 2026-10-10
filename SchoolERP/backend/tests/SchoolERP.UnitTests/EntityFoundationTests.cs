using SchoolERP.Domain.Common;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class EntityFoundationTests
{
    [Fact]
    public void DefaultIdentityUsesNonEmptyVersion7Guid()
    {
        var entity = new TestEntity();
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal('7', entity.Id.ToString()[14]);
    }

    [Fact]
    public void SameNonEmptyIdentityAndTypeAreEqual()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id); var second = new TestEntity(id);
        Assert.Equal(first, second); Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentIdentitiesAreNotEqual() => Assert.NotEqual(new TestEntity(), new TestEntity());

    [Fact]
    public void EmptyTransientEntitiesAreNotEqual() => Assert.NotEqual(new TestEntity(Guid.Empty), new TestEntity(Guid.Empty));

    [Fact]
    public void DifferentConcreteTypesWithSameIdentityAreNotEqual()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual<Entity>(new TestEntity(id), new OtherEntity(id));
    }

    [Fact]
    public void AuditableFieldsBeginUnpopulated()
    {
        var entity = new TestAuditableEntity();
        Assert.Equal(default, entity.CreatedAt);
        Assert.Null(entity.UpdatedAt); Assert.Null(entity.CreatedBy); Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public void DomainEventsCanBeAddedRemovedAndClearedWithoutExposingMutableStorage()
    {
        var entity = new TestEntity(); var first = new TestEvent(); var second = new TestEvent();
        entity.AddDomainEvent(first); entity.AddDomainEvent(second);
        Assert.Equal([first, second], entity.DomainEvents);
        Assert.True(((ICollection<IDomainEvent>)entity.DomainEvents).IsReadOnly);
        Assert.True(entity.RemoveDomainEvent(first)); Assert.Equal([second], entity.DomainEvents);
        entity.ClearDomainEvents(); Assert.Empty(entity.DomainEvents);
        Assert.Equal(DateTimeKind.Utc, first.OccurredOnUtc.Kind);
    }

    [Fact]
    public void TenantOwnershipAndSoftDeleteRemainExplicit()
    {
        Assert.False((object)Tenant.Create("Test", "test") is ITenantEntity);
        Assert.IsAssignableFrom<ITenantEntity>(School.Create(Guid.NewGuid(), "Test", "TEST"));
        Assert.IsAssignableFrom<ITenantEntity>(Campus.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", "TEST"));
        Assert.False((object)Tenant.Create("Test", "test-two") is ISoftDeletableEntity);
    }

    [Theory]
    [InlineData("tenant", "name")]
    [InlineData("tenant", "slug")]
    [InlineData("school", "name")]
    [InlineData("school", "code")]
    [InlineData("campus", "name")]
    [InlineData("campus", "code")]
    public void RequiredFoundationStringsRejectWhitespace(string entityName, string property)
    {
        Assert.Throws<DomainException>(() => SetRequiredProperty(entityName, property));
    }

    [Fact]
    public void CodesAreTrimmedAndUppercase()
    {
        Assert.Equal("MAIN", School.Create(Guid.NewGuid(), "Test", " main ").Code);
        Assert.Equal("NORTH", Campus.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", " north ").Code);
    }

    private static void SetRequiredProperty(string entityName, string property)
    {
        if (entityName == "tenant")
        {
            if (property == "name") Tenant.Create("   ", "valid"); else Tenant.Create("Valid", "   ");
        }
        else if (entityName == "school")
        {
            if (property == "name") School.Create(Guid.NewGuid(), "   ", "VALID");
            else School.Create(Guid.NewGuid(), "Valid", "   ");
        }
        else
        {
            if (property == "name") Campus.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", "VALID");
            else Campus.Create(Guid.NewGuid(), Guid.NewGuid(), "Valid", "   ");
        }
    }

    private sealed class TestEntity : Entity
    {
        public TestEntity() { }
        public TestEntity(Guid id) : base(id) { }
    }
    private sealed class OtherEntity(Guid id) : Entity(id);
    private sealed class TestAuditableEntity : AuditableEntity;
    private sealed record TestEvent : DomainEvent;
}
