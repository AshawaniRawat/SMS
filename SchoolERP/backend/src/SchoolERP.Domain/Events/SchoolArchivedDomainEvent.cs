namespace SchoolERP.Domain.Events;

public sealed record SchoolArchivedDomainEvent(Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
