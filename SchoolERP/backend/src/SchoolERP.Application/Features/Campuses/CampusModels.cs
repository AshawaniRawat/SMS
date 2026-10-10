using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Campuses;

public sealed record CampusDto(Guid Id, Guid TenantId, Guid SchoolId, string Name, string Code, string? ShortName,
    string? Email, string? Phone, string? AlternatePhone, string? AddressLine1, string? AddressLine2,
    string? City, string? State, string? PostalCode, string? CountryCode, string? TimeZoneId,
    decimal? Latitude, decimal? Longitude, bool IsMainCampus, CampusStatus Status,
    DateTime? ActivatedAt, DateTime? SuspendedAt, DateTime? ArchivedAt, DateTime CreatedAt, DateTime? UpdatedAt);

public record CampusProfileRequest(string Name, string? ShortName = null, string? Email = null, string? Phone = null,
    string? AlternatePhone = null, string? AddressLine1 = null, string? AddressLine2 = null, string? City = null,
    string? State = null, string? PostalCode = null, string? CountryCode = null, string? TimeZoneId = null,
    decimal? Latitude = null, decimal? Longitude = null);

public sealed record CreateCampusRequest(Guid SchoolId, string Name, string Code, bool? IsMainCampus = null,
    string? ShortName = null, string? Email = null, string? Phone = null, string? AlternatePhone = null,
    string? AddressLine1 = null, string? AddressLine2 = null, string? City = null, string? State = null,
    string? PostalCode = null, string? CountryCode = null, string? TimeZoneId = null,
    decimal? Latitude = null, decimal? Longitude = null)
    : CampusProfileRequest(Name, ShortName, Email, Phone, AlternatePhone, AddressLine1, AddressLine2, City, State,
        PostalCode, CountryCode, TimeZoneId, Latitude, Longitude);

public sealed record ChangeCampusCodeRequest(string Code);
