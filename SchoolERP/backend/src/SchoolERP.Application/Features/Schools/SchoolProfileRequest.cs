using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Schools;

public record SchoolProfileRequest(
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
    string? LogoUrl = null,
    string? TimeZoneId = null,
    string? DefaultCurrency = null,
    string? PrincipalName = null);

public sealed record CreateSchoolRequest(
    Guid TenantId,
    string Name,
    string Code,
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
    string? LogoUrl = null,
    string? TimeZoneId = null,
    string? DefaultCurrency = null,
    string? PrincipalName = null)
    : SchoolProfileRequest(Name, ShortName, RegistrationNumber, AffiliationNumber, BoardCode,
        BoardType, Email, Phone, AlternatePhone, Website, AddressLine1, AddressLine2, City,
        State, PostalCode, CountryCode, LogoUrl, TimeZoneId, DefaultCurrency, PrincipalName);
