namespace SchoolERP.Domain.Events;

public sealed record TenantArchivedDomainEvent(Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
