namespace SchoolERP.Domain.Events;

public sealed record CampusCreatedDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
public sealed record CampusActivatedDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
public sealed record CampusSuspendedDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
public sealed record CampusReactivatedDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
public sealed record CampusArchivedDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
public sealed record CampusMarkedAsMainDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
public sealed record CampusUpdatedDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
public sealed record CampusCodeChangedDomainEvent(Guid CampusId, Guid SchoolId, Guid TenantId, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);
