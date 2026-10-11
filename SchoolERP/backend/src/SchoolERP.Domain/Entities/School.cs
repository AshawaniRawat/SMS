using System.Text.RegularExpressions;
using SchoolERP.Domain.Common;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Entities;

public sealed partial class School : AuditableEntity, ITenantEntity, ISoftDeletableEntity, IAggregateRoot
{
    private string _name = string.Empty;
    private string _code = string.Empty;

    private School() { }

    public Guid TenantId { get; private set; }
    public string Name { get => _name; private set => _name = Required(value, 200, nameof(Name)); }
    public string Code { get => _code; private set => _code = NormalizeCode(value); }
    public string? ShortName { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string? AffiliationNumber { get; private set; }
    public string? BoardCode { get; private set; }
    public SchoolBoardType? BoardType { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? AlternatePhone { get; private set; }
    public string? Website { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? TimeZoneId { get; private set; }
    public string? DefaultCurrency { get; private set; }
    public string? PrincipalName { get; private set; }
    public SchoolStatus Status { get; private set; } = SchoolStatus.Draft;
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public long Version { get; set; }
    public Tenant Tenant { get; private set; } = null!;
    public ICollection<Campus> Campuses { get; } = new List<Campus>();
    public ICollection<AcademicYear> AcademicYears { get; } = new List<AcademicYear>();

    Guid ITenantEntity.TenantId
    {
        get => TenantId;
        set
        {
            if (TenantId != Guid.Empty && TenantId != value)
                throw new DomainException("School tenant ownership cannot be changed.");
            TenantId = value;
        }
    }

    public static School Create(
        Guid tenantId,
        string name,
        string code,
        SchoolBoardType? boardType = null,
        string? email = null,
        string? phone = null,
        string? countryCode = null,
        string? timeZoneId = null,
        string? defaultCurrency = null,
        DateTime? occurredOnUtc = null)
    {
        if (tenantId == Guid.Empty) throw new DomainException("School TenantId is required.");
        var school = CreateCore(tenantId, name, code, boardType, email, phone, countryCode, timeZoneId, defaultCurrency);
        school.AddDomainEvent(new SchoolCreatedDomainEvent(school.Id, tenantId, EnsureUtc(occurredOnUtc ?? DateTime.UtcNow)));
        return school;
    }

    public static School Create(Tenant tenant, string name, string code, DateTime? occurredOnUtc = null)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var school = Create(tenant.Id, name, code, occurredOnUtc: occurredOnUtc);
        school.Tenant = tenant;
        return school;
    }

    public static School CreateForCurrentTenant(string name, string code, DateTime? occurredOnUtc = null)
    {
        var school = CreateCore(Guid.Empty, name, code, null, null, null, null, null, null);
        school.AddDomainEvent(new SchoolCreatedDomainEvent(school.Id, Guid.Empty, EnsureUtc(occurredOnUtc ?? DateTime.UtcNow)));
        return school;
    }

    public void UpdateProfile(
        string name,
        string? shortName,
        string? registrationNumber,
        string? affiliationNumber,
        string? boardCode,
        SchoolBoardType? boardType,
        string? email,
        string? phone,
        string? alternatePhone,
        string? website,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? state,
        string? postalCode,
        string? countryCode,
        string? logoUrl,
        string? timeZoneId,
        string? defaultCurrency,
        string? principalName,
        DateTime? occurredOnUtc = null)
    {
        Name = name;
        ShortName = Optional(shortName, 100, nameof(ShortName));
        RegistrationNumber = Optional(registrationNumber, 100, nameof(RegistrationNumber));
        AffiliationNumber = Optional(affiliationNumber, 100, nameof(AffiliationNumber));
        BoardCode = Optional(boardCode, 100, nameof(BoardCode));
        BoardType = boardType;
        Email = Optional(email, 320, nameof(Email))?.ToLowerInvariant();
        Phone = Optional(phone, 30, nameof(Phone));
        AlternatePhone = Optional(alternatePhone, 30, nameof(AlternatePhone));
        Website = Optional(website, 500, nameof(Website));
        AddressLine1 = Optional(addressLine1, 250, nameof(AddressLine1));
        AddressLine2 = Optional(addressLine2, 250, nameof(AddressLine2));
        City = Optional(city, 100, nameof(City));
        State = Optional(state, 100, nameof(State));
        PostalCode = Optional(postalCode, 20, nameof(PostalCode));
        CountryCode = FixedCode(countryCode, 2, nameof(CountryCode));
        LogoUrl = Optional(logoUrl, 1000, nameof(LogoUrl));
        TimeZoneId = Optional(timeZoneId, 100, nameof(TimeZoneId));
        DefaultCurrency = FixedCode(defaultCurrency, 3, nameof(DefaultCurrency));
        PrincipalName = Optional(principalName, 200, nameof(PrincipalName));
        if (occurredOnUtc.HasValue)
            AddDomainEvent(new SchoolProfileUpdatedDomainEvent(Id, TenantId, EnsureUtc(occurredOnUtc.Value)));
    }

    public void ChangeCode(string code) => Code = code;

    public void Activate(DateTime occurredOnUtc)
    {
        if (Status == SchoolStatus.Active) return;
        if (Status != SchoolStatus.Draft) throw new InvalidSchoolStatusTransitionException(Status, SchoolStatus.Active);
        var now = EnsureUtc(occurredOnUtc);
        Status = SchoolStatus.Active;
        ActivatedAt ??= now;
        SuspendedAt = null;
        AddDomainEvent(new SchoolActivatedDomainEvent(Id, TenantId, now));
    }

    public void Suspend(DateTime occurredOnUtc)
    {
        if (Status == SchoolStatus.Suspended) return;
        if (Status != SchoolStatus.Active) throw new InvalidSchoolStatusTransitionException(Status, SchoolStatus.Suspended);
        var now = EnsureUtc(occurredOnUtc);
        Status = SchoolStatus.Suspended;
        SuspendedAt = now;
        AddDomainEvent(new SchoolSuspendedDomainEvent(Id, TenantId, now));
    }

    public void Reactivate(DateTime occurredOnUtc)
    {
        if (Status == SchoolStatus.Active) return;
        if (Status != SchoolStatus.Suspended) throw new InvalidSchoolStatusTransitionException(Status, SchoolStatus.Active);
        var now = EnsureUtc(occurredOnUtc);
        Status = SchoolStatus.Active;
        ActivatedAt ??= now;
        SuspendedAt = null;
        AddDomainEvent(new SchoolReactivatedDomainEvent(Id, TenantId, now));
    }

    public void Archive(DateTime occurredOnUtc)
    {
        if (Status == SchoolStatus.Archived) return;
        if (Status is not (SchoolStatus.Draft or SchoolStatus.Active or SchoolStatus.Suspended))
            throw new InvalidSchoolStatusTransitionException(Status, SchoolStatus.Archived);
        var now = EnsureUtc(occurredOnUtc);
        Status = SchoolStatus.Archived;
        ArchivedAt = now;
        AddDomainEvent(new SchoolArchivedDomainEvent(Id, TenantId, now));
    }

    public static string NormalizeCode(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is 0 or > 50 || !CodePattern().IsMatch(normalized))
            throw new DomainException("School code must be 1–50 uppercase letters, digits, hyphens, or underscores.");
        return normalized;
    }

    private static School CreateCore(Guid tenantId, string name, string code, SchoolBoardType? boardType,
        string? email, string? phone, string? countryCode, string? timeZoneId, string? defaultCurrency)
    {
        var school = new School { TenantId = tenantId, Name = name, Code = code };
        school.UpdateProfile(school.Name, null, null, null, null, boardType, email, phone, null, null,
            null, null, null, null, null, countryCode, null, timeZoneId, defaultCurrency, null);
        return school;
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string Required(string? value, int maxLength, string propertyName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) throw new DomainException($"School {propertyName} is required.");
        if (normalized.Length > maxLength) throw new DomainException($"School {propertyName} cannot exceed {maxLength} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int maxLength, string propertyName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainException($"School {propertyName} cannot exceed {maxLength} characters.");
        return normalized;
    }

    private static string? FixedCode(string? value, int length, string propertyName)
    {
        var normalized = Optional(value, length, propertyName)?.ToUpperInvariant();
        if (normalized is not null && normalized.Length != length)
            throw new DomainException($"School {propertyName} must contain exactly {length} characters.");
        return normalized;
    }

    [GeneratedRegex("\\A[A-Z0-9]+(?:[-_][A-Z0-9]+)*\\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex CodePattern();
}
