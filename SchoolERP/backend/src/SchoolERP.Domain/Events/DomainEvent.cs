using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Events;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent() => OccurredOnUtc = DateTime.UtcNow;
    protected DomainEvent(DateTime occurredOnUtc) => OccurredOnUtc = occurredOnUtc;
    public DateTime OccurredOnUtc { get; init; }
}
