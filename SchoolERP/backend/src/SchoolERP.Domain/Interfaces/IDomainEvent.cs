namespace SchoolERP.Domain.Interfaces;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
