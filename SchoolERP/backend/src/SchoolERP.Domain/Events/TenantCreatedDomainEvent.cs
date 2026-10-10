namespace SchoolERP.Domain.Events;

public sealed record TenantCreatedDomainEvent(Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
