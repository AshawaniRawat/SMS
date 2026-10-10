using System.Text.RegularExpressions;
using SchoolERP.Domain.Common;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Entities;

public sealed partial class Campus : AuditableEntity, ITenantEntity, ISoftDeletableEntity
{
    private string _name = string.Empty;
    private string _code = string.Empty;
    private Campus() { }

    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Name { get => _name; private set => _name = Required(value, 200, nameof(Name)); }
    public string Code { get => _code; private set => _code = NormalizeCode(value); }
    public string? ShortName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? AlternatePhone { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public string? TimeZoneId { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public CampusStatus Status { get; private set; } = CampusStatus.Draft;
    public bool IsMainCampus { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Tenant Tenant { get; private set; } = null!;
    public School School { get; private set; } = null!;

    Guid ITenantEntity.TenantId { get => TenantId; set { if (TenantId != Guid.Empty && TenantId != value) throw new DomainException("Campus tenant ownership cannot be changed."); TenantId = value; } }

    public static Campus Create(Guid tenantId, Guid schoolId, string name, string code, bool isMainCampus = false,
        string? email = null, string? phone = null, string? countryCode = null, string? timeZoneId = null, DateTime? occurredOnUtc = null)
    {
        if (tenantId == Guid.Empty) throw new DomainException("Campus TenantId is required.");
        if (schoolId == Guid.Empty) throw new DomainException("Campus SchoolId is required.");
        var campus = CreateCore(tenantId, schoolId, name, code, isMainCampus, email, phone, countryCode, timeZoneId);
        campus.AddDomainEvent(new CampusCreatedDomainEvent(campus.Id, schoolId, tenantId, Utc(occurredOnUtc ?? DateTime.UtcNow)));
        return campus;
    }

    public static Campus Create(School school, string name, string code, bool isMainCampus = false, DateTime? occurredOnUtc = null)
    {
        ArgumentNullException.ThrowIfNull(school);
        var campus = Create(school.TenantId, school.Id, name, code, isMainCampus, occurredOnUtc: occurredOnUtc);
        campus.School = school;
        return campus;
    }

    public static Campus CreateForCurrentTenant(Guid schoolId, string name, string code, bool isMainCampus = false, DateTime? occurredOnUtc = null)
    {
        if (schoolId == Guid.Empty) throw new DomainException("Campus SchoolId is required.");
        var campus = CreateCore(Guid.Empty, schoolId, name, code, isMainCampus, null, null, null, null);
        campus.AddDomainEvent(new CampusCreatedDomainEvent(campus.Id, schoolId, Guid.Empty, Utc(occurredOnUtc ?? DateTime.UtcNow)));
        return campus;
    }

    public void UpdateProfile(string name, string? shortName, string? email, string? phone, string? alternatePhone,
        string? addressLine1, string? addressLine2, string? city, string? state, string? postalCode,
        string? countryCode, string? timeZoneId, decimal? latitude, decimal? longitude)
    {
        Name = name; ShortName = Optional(shortName, 100, nameof(ShortName)); Email = Optional(email, 320, nameof(Email))?.ToLowerInvariant();
        Phone = Optional(phone, 30, nameof(Phone)); AlternatePhone = Optional(alternatePhone, 30, nameof(AlternatePhone));
        AddressLine1 = Optional(addressLine1, 250, nameof(AddressLine1)); AddressLine2 = Optional(addressLine2, 250, nameof(AddressLine2));
        City = Optional(city, 100, nameof(City)); State = Optional(state, 100, nameof(State)); PostalCode = Optional(postalCode, 20, nameof(PostalCode));
        CountryCode = FixedCode(countryCode, 2, nameof(CountryCode)); TimeZoneId = Optional(timeZoneId, 100, nameof(TimeZoneId));
        if (latitude is < -90 or > 90) throw new DomainException("Campus Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180) throw new DomainException("Campus Longitude must be between -180 and 180.");
        Latitude = latitude; Longitude = longitude;
    }

    public void ChangeCode(string code) => Code = code;
    public void MarkAsMainCampus(DateTime occurredOnUtc) { if (IsMainCampus) return; IsMainCampus = true; AddDomainEvent(new CampusMarkedAsMainDomainEvent(Id, SchoolId, TenantId, Utc(occurredOnUtc))); }
    public void RemoveMainCampusDesignation() => IsMainCampus = false;
    public void Activate(DateTime occurredOnUtc) => ChangeStatus(CampusStatus.Draft, CampusStatus.Active, occurredOnUtc);
    public void Suspend(DateTime occurredOnUtc) => ChangeStatus(CampusStatus.Active, CampusStatus.Suspended, occurredOnUtc);
    public void Reactivate(DateTime occurredOnUtc) => ChangeStatus(CampusStatus.Suspended, CampusStatus.Active, occurredOnUtc, true);
    public void Archive(DateTime occurredOnUtc) { if (Status == CampusStatus.Archived) return; if (Status is not (CampusStatus.Draft or CampusStatus.Active or CampusStatus.Suspended)) throw new InvalidCampusStatusTransitionException(Status, CampusStatus.Archived); var now = Utc(occurredOnUtc); Status = CampusStatus.Archived; ArchivedAt = now; AddDomainEvent(new CampusArchivedDomainEvent(Id, SchoolId, TenantId, now)); }

    public static string NormalizeCode(string? value) { var normalized = value?.Trim().ToUpperInvariant() ?? ""; if (normalized.Length is 0 or > 50 || !CodePattern().IsMatch(normalized)) throw new DomainException("Campus code must be 1-50 uppercase letters, digits, hyphens, or underscores."); return normalized; }
    private void ChangeStatus(CampusStatus from, CampusStatus to, DateTime occurredOnUtc, bool reactivation = false) { if (Status == to) return; if (Status != from) throw new InvalidCampusStatusTransitionException(Status, to); var now = Utc(occurredOnUtc); Status = to; if (to == CampusStatus.Active) { ActivatedAt ??= now; SuspendedAt = null; } else SuspendedAt = now; AddDomainEvent(reactivation ? new CampusReactivatedDomainEvent(Id, SchoolId, TenantId, now) : to == CampusStatus.Active ? new CampusActivatedDomainEvent(Id, SchoolId, TenantId, now) : new CampusSuspendedDomainEvent(Id, SchoolId, TenantId, now)); }
    private static Campus CreateCore(Guid tenantId, Guid schoolId, string name, string code, bool main, string? email, string? phone, string? countryCode, string? timeZoneId) { var campus = new Campus { TenantId = tenantId, SchoolId = schoolId, Name = name, Code = code, IsMainCampus = main }; campus.UpdateProfile(campus.Name, null, email, phone, null, null, null, null, null, null, countryCode, timeZoneId, null, null); return campus; }
    private static DateTime Utc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
    private static string Required(string? value, int max, string property) { var v = value?.Trim() ?? ""; if (v.Length == 0) throw new DomainException($"Campus {property} is required."); if (v.Length > max) throw new DomainException($"Campus {property} cannot exceed {max} characters."); return v; }
    private static string? Optional(string? value, int max, string property) { var v = value?.Trim(); if (string.IsNullOrEmpty(v)) return null; if (v.Length > max) throw new DomainException($"Campus {property} cannot exceed {max} characters."); return v; }
    private static string? FixedCode(string? value, int length, string property) { var v = Optional(value, length, property)?.ToUpperInvariant(); if (v is not null && v.Length != length) throw new DomainException($"Campus {property} must contain exactly {length} characters."); return v; }
    [GeneratedRegex("\\A[A-Z0-9]+(?:[-_][A-Z0-9]+)*\\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)] private static partial Regex CodePattern();
}
