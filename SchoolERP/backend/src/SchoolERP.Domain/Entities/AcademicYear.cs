using System.Text.RegularExpressions;
using SchoolERP.Domain.Common;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Entities;

public sealed partial class AcademicYear : AuditableEntity, ITenantEntity, IAggregateRoot
{
    public const int MaximumDurationMonths = 24;
    private string _name = string.Empty;
    private string _code = string.Empty;
    private AcademicYear() { }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Name { get => _name; private set => _name = Required(value, 100, nameof(Name)); }
    public string Code { get => _code; private set => _code = NormalizeCode(value); }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public AcademicYearStatus Status { get; private set; } = AcademicYearStatus.Draft;
    public bool IsCurrent { get; private set; }
    public bool IsAdmissionOpen { get; private set; }
    public DateOnly? AdmissionStartDate { get; private set; }
    public DateOnly? AdmissionEndDate { get; private set; }
    public DateOnly? LockDate { get; private set; }
    public string? Description { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public School School { get; private set; } = null!;

    Guid ITenantEntity.TenantId { get => TenantId; set { if (TenantId != Guid.Empty && TenantId != value) throw new DomainException("Academic year tenant ownership cannot be changed."); TenantId = value; } }

    public static AcademicYear Create(Guid tenantId, Guid schoolId, string name, string code, DateOnly startDate,
        DateOnly endDate, string? description = null, DateTime? occurredOnUtc = null)
    {
        if (tenantId == Guid.Empty) throw new DomainException("Academic year TenantId is required.");
        if (schoolId == Guid.Empty) throw new DomainException("Academic year SchoolId is required.");
        ValidateDates(startDate, endDate);
        var year = new AcademicYear { TenantId = tenantId, SchoolId = schoolId, Name = name, Code = code,
            StartDate = startDate, EndDate = endDate, Description = Optional(description, 1000, nameof(Description)) };
        year.AddDomainEvent(new AcademicYearCreatedDomainEvent(year.Id, schoolId, tenantId, Utc(occurredOnUtc ?? DateTime.UtcNow)));
        return year;
    }

    public static AcademicYear Create(School school, string name, string code, DateOnly startDate, DateOnly endDate,
        string? description = null, DateTime? occurredOnUtc = null)
    { ArgumentNullException.ThrowIfNull(school); var year = Create(school.TenantId, school.Id, name, code, startDate, endDate, description, occurredOnUtc); year.School = school; return year; }

    public void UpdateDetails(string name, DateOnly startDate, DateOnly endDate, string? description,
        DateOnly? admissionStartDate, DateOnly? admissionEndDate, DateOnly? lockDate)
    {
        if (Status != AcademicYearStatus.Draft) throw new DomainException("Core academic year details can only be changed while Draft.");
        ValidateDates(startDate, endDate); ValidateAdmissionRange(admissionStartDate, admissionEndDate);
        Name = name; StartDate = startDate; EndDate = endDate; Description = Optional(description, 1000, nameof(Description));
        AdmissionStartDate = admissionStartDate; AdmissionEndDate = admissionEndDate; SetLockDate(lockDate);
    }
    public void UpdateActiveMetadata(string name,string? description,DateOnly? admissionStartDate,DateOnly? admissionEndDate,DateOnly? lockDate,DateTime occurredOnUtc){if(Status!=AcademicYearStatus.Active)throw new DomainException("Metadata can only be updated for an Active academic year.");ValidateAdmissionRange(admissionStartDate,admissionEndDate);Name=name;Description=Optional(description,1000,nameof(Description));AdmissionStartDate=admissionStartDate;AdmissionEndDate=admissionEndDate;SetLockDate(lockDate);AddDomainEvent(new AcademicYearUpdatedDomainEvent(Id,SchoolId,TenantId,Utc(occurredOnUtc)));}
    public void RecordUpdated(DateTime occurredOnUtc)=>AddDomainEvent(new AcademicYearUpdatedDomainEvent(Id,SchoolId,TenantId,Utc(occurredOnUtc)));
    public void ChangeCode(string code,DateTime? occurredOnUtc=null) { if (Status != AcademicYearStatus.Draft) throw new DomainException("Academic year code can only be changed while Draft."); Code = code;if(occurredOnUtc.HasValue)AddDomainEvent(new AcademicYearCodeChangedDomainEvent(Id,SchoolId,TenantId,Utc(occurredOnUtc.Value))); }
    public void Activate(DateTime occurredOnUtc) { if (Status == AcademicYearStatus.Active) return; Transition(AcademicYearStatus.Draft, AcademicYearStatus.Active); AddDomainEvent(new AcademicYearActivatedDomainEvent(Id, SchoolId, TenantId, Utc(occurredOnUtc))); }
    public void Close(DateTime occurredOnUtc) { Transition(AcademicYearStatus.Active, AcademicYearStatus.Closed); IsCurrent = false; IsAdmissionOpen = false; AddDomainEvent(new AcademicYearClosedDomainEvent(Id, SchoolId, TenantId, Utc(occurredOnUtc))); }
    public void Archive(DateTime occurredOnUtc) { if (Status == AcademicYearStatus.Archived) return; if (Status is not (AcademicYearStatus.Draft or AcademicYearStatus.Closed)) throw new InvalidAcademicYearStatusTransitionException(Status, AcademicYearStatus.Archived); Status = AcademicYearStatus.Archived; IsCurrent = false; IsAdmissionOpen = false; AddDomainEvent(new AcademicYearArchivedDomainEvent(Id, SchoolId, TenantId, Utc(occurredOnUtc))); }
    public void MarkAsCurrent(DateTime occurredOnUtc) { if (Status != AcademicYearStatus.Active) throw new DomainException("Only an Active academic year can be current."); if (IsCurrent) return; IsCurrent = true; AddDomainEvent(new AcademicYearMarkedCurrentDomainEvent(Id, SchoolId, TenantId, Utc(occurredOnUtc))); }
    public void UnmarkAsCurrent() => IsCurrent = false;
    public void OpenAdmissions(DateOnly? startDate, DateOnly? endDate, DateTime occurredOnUtc) { if (Status is not (AcademicYearStatus.Draft or AcademicYearStatus.Active)) throw new DomainException("Admissions can only open for a Draft or Active academic year."); ValidateAdmissionRange(startDate, endDate); AdmissionStartDate = startDate; AdmissionEndDate = endDate; IsAdmissionOpen = true; AddDomainEvent(new AcademicYearAdmissionsOpenedDomainEvent(Id, SchoolId, TenantId, Utc(occurredOnUtc))); }
    public void CloseAdmissions(DateTime occurredOnUtc) { if (!IsAdmissionOpen) return; IsAdmissionOpen = false; AddDomainEvent(new AcademicYearAdmissionsClosedDomainEvent(Id, SchoolId, TenantId, Utc(occurredOnUtc))); }
    public void SetLockDate(DateOnly? lockDate) { if (lockDate.HasValue && (lockDate < StartDate || lockDate > EndDate)) throw new DomainException("Academic year LockDate must be between StartDate and EndDate."); LockDate = lockDate; }
    public void SetLockDate(DateOnly? lockDate,DateTime occurredOnUtc){SetLockDate(lockDate);AddDomainEvent(new AcademicYearLockDateChangedDomainEvent(Id,SchoolId,TenantId,Utc(occurredOnUtc)));}

    public static string NormalizeCode(string? value) { var v = value?.Trim().ToUpperInvariant() ?? ""; if (v.Length is 0 or > 50 || !CodePattern().IsMatch(v)) throw new DomainException("Academic year code must be 1-50 uppercase letters, digits, hyphens, or underscores."); return v; }
    public static void ValidateDates(DateOnly start, DateOnly end) { if (start == default) throw new DomainException("Academic year StartDate is required."); if (end <= start) throw new DomainException("Academic year EndDate must be after StartDate."); if (end > start.AddMonths(MaximumDurationMonths)) throw new DomainException($"Academic year cannot exceed {MaximumDurationMonths} months."); }
    private static void ValidateAdmissionRange(DateOnly? start, DateOnly? end) { if (start.HasValue && end.HasValue && end < start) throw new DomainException("AdmissionEndDate cannot be before AdmissionStartDate."); }
    private void Transition(AcademicYearStatus from, AcademicYearStatus to) { if (Status == to) return; if (Status != from) throw new InvalidAcademicYearStatusTransitionException(Status, to); Status = to; }
    private static DateTime Utc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
    private static string Required(string? value, int max, string property) { var v = value?.Trim() ?? ""; if (v.Length == 0) throw new DomainException($"Academic year {property} is required."); if (v.Length > max) throw new DomainException($"Academic year {property} cannot exceed {max} characters."); return v; }
    private static string? Optional(string? value, int max, string property) { var v = value?.Trim(); if (string.IsNullOrEmpty(v)) return null; if (v.Length > max) throw new DomainException($"Academic year {property} cannot exceed {max} characters."); return v; }
    [GeneratedRegex("\\A[A-Z0-9]+(?:[-_][A-Z0-9]+)*\\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)] private static partial Regex CodePattern();
}
