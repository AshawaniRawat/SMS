namespace SchoolERP.Domain.Events;

public sealed record TenantActivatedDomainEvent(Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
