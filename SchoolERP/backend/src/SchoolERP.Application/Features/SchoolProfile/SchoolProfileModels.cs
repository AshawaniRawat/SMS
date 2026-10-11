using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.SchoolProfile;

public sealed record SchoolProfileDto(
    Guid Id, string Name, string Code, string? ShortName,
    string? RegistrationNumber, string? AffiliationNumber, string? BoardCode,
    SchoolBoardType? BoardType, string? Email, string? Phone, string? AlternatePhone,
    string? Website, string? AddressLine1, string? AddressLine2, string? City,
    string? State, string? PostalCode, string? CountryCode, string? LogoUrl,
    string? TimeZoneId, string? DefaultCurrency, string? PrincipalName,
    SchoolStatus Status, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record UpdateSchoolProfileRequest(
    string Name,
    string? ShortName = null,
    string? RegistrationNumber = null,
    string? AffiliationNumber = null,
    string? BoardCode = null,
    SchoolBoardType? BoardType = null,
    string? Email = null,
    string? Phone = null,
    string? AlternatePhone = null,
    string? Website = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? CountryCode = null,
    string? TimeZoneId = null,
    string? DefaultCurrency = null,
    string? PrincipalName = null);
