namespace SchoolERP.Domain.Events;

public sealed record SchoolReactivatedDomainEvent(Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
