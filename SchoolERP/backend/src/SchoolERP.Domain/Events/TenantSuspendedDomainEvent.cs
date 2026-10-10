namespace SchoolERP.Domain.Events;

public sealed record TenantSuspendedDomainEvent(Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
