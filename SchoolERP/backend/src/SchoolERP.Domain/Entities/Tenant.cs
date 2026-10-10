using System.Text.RegularExpressions;
using SchoolERP.Domain.Common;
using SchoolERP.Domain.Constants;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Events;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Entities;

public sealed partial class Tenant : AuditableEntity, IAggregateRoot
{
    private string _name = string.Empty;
    private string _slug = string.Empty;

    private Tenant() { }

    public string Name { get => _name; private set => _name = Required(value, 200, nameof(Name)); }
    public string Slug { get => _slug; private set => _slug = NormalizeSlug(value); }
    public string? LegalName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? TimeZoneId { get; private set; }
    public string? DefaultCurrency { get; private set; }
    public string? CountryCode { get; private set; }
    public TenantStatus Status { get; private set; } = TenantStatus.Pending;
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public ICollection<School> Schools { get; } = new List<School>();
    public ICollection<Campus> Campuses { get; } = new List<Campus>();

    public static Tenant Create(
        string name,
        string slug,
        string? email = null,
        string? phone = null,
        string? countryCode = null,
        string? timeZoneId = null,
        DateTime? occurredOnUtc = null)
    {
        var tenant = new Tenant { Name = name, Slug = slug };
        tenant.UpdateProfile(name, null, email, phone, null, null, countryCode, timeZoneId, null);
        tenant.AddDomainEvent(new TenantCreatedDomainEvent(tenant.Id, EnsureUtc(occurredOnUtc ?? DateTime.UtcNow)));
        return tenant;
    }

    public void UpdateProfile(
        string name,
        string? legalName,
        string? email,
        string? phone,
        string? website,
        string? logoUrl,
        string? countryCode,
        string? timeZoneId,
        string? defaultCurrency)
    {
        Name = name;
        LegalName = Optional(legalName, 250, nameof(LegalName));
        Email = Optional(email, 320, nameof(Email))?.ToLowerInvariant();
        Phone = Optional(phone, 30, nameof(Phone));
        Website = Optional(website, 500, nameof(Website));
        LogoUrl = Optional(logoUrl, 1000, nameof(LogoUrl));
        CountryCode = FixedCode(countryCode, 2, nameof(CountryCode));
        TimeZoneId = Optional(timeZoneId, 100, nameof(TimeZoneId));
        DefaultCurrency = FixedCode(defaultCurrency, 3, nameof(DefaultCurrency));
    }

    public void ChangeSlug(string slug) => Slug = slug;

    public void Activate(DateTime occurredOnUtc)
    {
        if (Status == TenantStatus.Active) return;
        EnsureTransition(TenantStatus.Pending, TenantStatus.Active);
        var now = EnsureUtc(occurredOnUtc);
        Status = TenantStatus.Active;
        ActivatedAt ??= now;
        SuspendedAt = null;
        ArchivedAt = null;
        AddDomainEvent(new TenantActivatedDomainEvent(Id, now));
    }

    public void Suspend(DateTime occurredOnUtc)
    {
        if (Status == TenantStatus.Suspended) return;
        EnsureTransition(TenantStatus.Active, TenantStatus.Suspended);
        var now = EnsureUtc(occurredOnUtc);
        Status = TenantStatus.Suspended;
        SuspendedAt = now;
        AddDomainEvent(new TenantSuspendedDomainEvent(Id, now));
    }

    public void Reactivate(DateTime occurredOnUtc)
    {
        if (Status == TenantStatus.Active) return;
        EnsureTransition(TenantStatus.Suspended, TenantStatus.Active);
        var now = EnsureUtc(occurredOnUtc);
        Status = TenantStatus.Active;
        ActivatedAt ??= now;
        SuspendedAt = null;
        AddDomainEvent(new TenantReactivatedDomainEvent(Id, now));
    }

    public void Archive(DateTime occurredOnUtc)
    {
        if (Status == TenantStatus.Archived) return;
        if (Status is not (TenantStatus.Pending or TenantStatus.Active or TenantStatus.Suspended))
            throw new InvalidTenantStatusTransitionException(Status, TenantStatus.Archived);
        var now = EnsureUtc(occurredOnUtc);
        Status = TenantStatus.Archived;
        ArchivedAt = now;
        AddDomainEvent(new TenantArchivedDomainEvent(Id, now));
    }

    public static string NormalizeSlug(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length is 0 or > 100 || !SlugPattern().IsMatch(normalized))
            throw new DomainException("Tenant slug must be 1–100 lowercase letters, digits or single hyphen-separated segments.");
        if (ReservedTenantSlugs.Contains(normalized))
            throw new DomainException("The requested tenant slug is reserved.");
        return normalized;
    }

    private void EnsureTransition(TenantStatus required, TenantStatus target)
    {
        if (Status != required) throw new InvalidTenantStatusTransitionException(Status, target);
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
        if (normalized.Length == 0) throw new DomainException($"Tenant {propertyName} is required.");
        if (normalized.Length > maxLength) throw new DomainException($"Tenant {propertyName} cannot exceed {maxLength} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int maxLength, string propertyName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainException($"Tenant {propertyName} cannot exceed {maxLength} characters.");
        return normalized;
    }

    private static string? FixedCode(string? value, int length, string propertyName)
    {
        var normalized = Optional(value, length, propertyName)?.ToUpperInvariant();
        if (normalized is not null && normalized.Length != length)
            throw new DomainException($"Tenant {propertyName} must contain exactly {length} characters.");
        return normalized;
    }

    [GeneratedRegex("\\A[a-z0-9]+(-[a-z0-9]+)*\\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex SlugPattern();
}
