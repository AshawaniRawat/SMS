using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Campuses;

public sealed record CampusDto(Guid Id, Guid TenantId, Guid SchoolId, string Name, string Code, string? ShortName,
    string? Email, string? Phone, string? AlternatePhone, string? AddressLine1, string? AddressLine2,
    string? City, string? State, string? PostalCode, string? CountryCode, string? TimeZoneId,
    decimal? Latitude, decimal? Longitude, bool IsMainCampus, CampusStatus Status,
    DateTime? ActivatedAt, DateTime? SuspendedAt, DateTime? ArchivedAt, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record CampusListItemDto(Guid Id, string Name, string Code, string? ShortName,
    CampusStatus Status, bool IsMainCampus, string? City, string? State, string? CountryCode,
    string? Phone, string? Email, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record CampusDetailsDto(Guid Id, string Name, string Code, string? ShortName,
    string? Email, string? Phone, string? AlternatePhone, string? AddressLine1, string? AddressLine2,
    string? City, string? State, string? PostalCode, string? CountryCode, string? TimeZoneId,
    decimal? Latitude, decimal? Longitude, CampusStatus Status, bool IsMainCampus,
    DateTime? ActivatedAt, DateTime? SuspendedAt, DateTime? ArchivedAt, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record GetCampusesQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    CampusStatus? Status = null, bool? IsMain = null, string? City = null,
    string? SortBy = null, string? SortDirection = null);

public record CampusProfileRequest(string Name, string? ShortName = null, string? Email = null, string? Phone = null,
    string? AlternatePhone = null, string? AddressLine1 = null, string? AddressLine2 = null, string? City = null,
    string? State = null, string? PostalCode = null, string? CountryCode = null, string? TimeZoneId = null,
    decimal? Latitude = null, decimal? Longitude = null);

public sealed record CreateCampusRequest(string Name, string Code, bool IsMainCampus = false,
    string? ShortName = null, string? Email = null, string? Phone = null, string? AlternatePhone = null,
    string? AddressLine1 = null, string? AddressLine2 = null, string? City = null, string? State = null,
    string? PostalCode = null, string? CountryCode = null, string? TimeZoneId = null,
    decimal? Latitude = null, decimal? Longitude = null)
    : CampusProfileRequest(Name, ShortName, Email, Phone, AlternatePhone, AddressLine1, AddressLine2, City, State,
        PostalCode, CountryCode, TimeZoneId, Latitude, Longitude)
{
    public CreateCampusRequest(Guid _, string name, string code, bool? isMainCampus = null,
        string? shortName = null, string? email = null, string? phone = null, string? alternatePhone = null,
        string? addressLine1 = null, string? addressLine2 = null, string? city = null, string? state = null,
        string? postalCode = null, string? countryCode = null, string? timeZoneId = null,
        decimal? latitude = null, decimal? longitude = null)
        : this(name, code, isMainCampus ?? false, shortName, email, phone, alternatePhone, addressLine1,
            addressLine2, city, state, postalCode, countryCode, timeZoneId, latitude, longitude) { }
}

public sealed record ChangeCampusCodeRequest(string Code);
