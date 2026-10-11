namespace SchoolERP.Domain.Events;
public sealed record DepartmentCreatedDomainEvent(Guid DepartmentId,Guid SchoolId,Guid? CampusId,Guid TenantId,DateTime OccurredOnUtc):DomainEvent(OccurredOnUtc);
public sealed record DepartmentUpdatedDomainEvent(Guid DepartmentId,Guid SchoolId,Guid? CampusId,Guid TenantId,DateTime OccurredOnUtc):DomainEvent(OccurredOnUtc);
public sealed record DepartmentCodeChangedDomainEvent(Guid DepartmentId,Guid SchoolId,Guid? CampusId,Guid TenantId,DateTime OccurredOnUtc):DomainEvent(OccurredOnUtc);
public sealed record DepartmentActivatedDomainEvent(Guid DepartmentId,Guid SchoolId,Guid? CampusId,Guid TenantId,DateTime OccurredOnUtc):DomainEvent(OccurredOnUtc);
public sealed record DepartmentDeactivatedDomainEvent(Guid DepartmentId,Guid SchoolId,Guid? CampusId,Guid TenantId,DateTime OccurredOnUtc):DomainEvent(OccurredOnUtc);
public sealed record DepartmentArchivedDomainEvent(Guid DepartmentId,Guid SchoolId,Guid? CampusId,Guid TenantId,DateTime OccurredOnUtc):DomainEvent(OccurredOnUtc);
