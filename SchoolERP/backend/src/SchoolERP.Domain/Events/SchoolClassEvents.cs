namespace SchoolERP.Domain.Events;
public sealed record SchoolClassCreatedDomainEvent(Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAtUtc):DomainEvent(OccurredAtUtc);
public sealed record SchoolClassUpdatedDomainEvent(Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAtUtc):DomainEvent(OccurredAtUtc);
public sealed record SchoolClassCodeChangedDomainEvent(Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAtUtc):DomainEvent(OccurredAtUtc);
public sealed record SchoolClassActivatedDomainEvent(Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAtUtc):DomainEvent(OccurredAtUtc);
public sealed record SchoolClassDeactivatedDomainEvent(Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAtUtc):DomainEvent(OccurredAtUtc);
public sealed record SchoolClassArchivedDomainEvent(Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAtUtc):DomainEvent(OccurredAtUtc);
