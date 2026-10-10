using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Common;

public abstract class Entity : IEquatable<Entity>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity() : this(Guid.CreateVersion7()) { }
    protected Entity(Guid id) => Id = id;

    public Guid Id { get; protected set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent ?? throw new ArgumentNullException(nameof(domainEvent)));
    public bool RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Entity? other) => ReferenceEquals(this, other) ||
        other is not null && GetType() == other.GetType() && Id != Guid.Empty && other.Id != Guid.Empty && Id == other.Id;
    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
    public override int GetHashCode() => Id == Guid.Empty ? base.GetHashCode() : HashCode.Combine(GetType(), Id);
    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
