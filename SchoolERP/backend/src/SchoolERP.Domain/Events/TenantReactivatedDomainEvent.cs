namespace SchoolERP.Domain.Events;

public sealed record TenantReactivatedDomainEvent(Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
