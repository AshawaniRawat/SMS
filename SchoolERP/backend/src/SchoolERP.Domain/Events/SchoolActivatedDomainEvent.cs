namespace SchoolERP.Domain.Events;

public sealed record SchoolActivatedDomainEvent(Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
