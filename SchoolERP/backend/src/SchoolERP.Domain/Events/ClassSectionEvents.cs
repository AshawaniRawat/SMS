namespace SchoolERP.Domain.Events;
public sealed record SectionCreatedDomainEvent(Guid SectionId,Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAt):DomainEvent(OccurredAt);
public sealed record SectionUpdatedDomainEvent(Guid SectionId,Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAt):DomainEvent(OccurredAt);
public sealed record SectionCodeChangedDomainEvent(Guid SectionId,Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAt):DomainEvent(OccurredAt);
public sealed record SectionActivatedDomainEvent(Guid SectionId,Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAt):DomainEvent(OccurredAt);
public sealed record SectionDeactivatedDomainEvent(Guid SectionId,Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAt):DomainEvent(OccurredAt);
public sealed record SectionArchivedDomainEvent(Guid SectionId,Guid ClassId,Guid AcademicYearId,Guid SchoolId,Guid TenantId,DateTime OccurredAt):DomainEvent(OccurredAt);
