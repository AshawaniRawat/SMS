namespace SchoolERP.Domain.Events;

public sealed record SchoolSuspendedDomainEvent(Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
