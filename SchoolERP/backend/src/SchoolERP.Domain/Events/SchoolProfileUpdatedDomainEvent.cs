namespace SchoolERP.Domain.Events;

public sealed record SchoolProfileUpdatedDomainEvent(Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);
