using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Schools;

public sealed record SchoolDto(
    Guid Id, Guid TenantId, string Name, string Code, string? ShortName,
    string? RegistrationNumber, string? AffiliationNumber, string? BoardCode,
    SchoolBoardType? BoardType, string? Email, string? Phone, string? AlternatePhone,
    string? Website, string? AddressLine1, string? AddressLine2, string? City,
    string? State, string? PostalCode, string? CountryCode, string? LogoUrl,
    string? TimeZoneId, string? DefaultCurrency, string? PrincipalName,
    SchoolStatus Status, DateTime? ActivatedAt, DateTime? SuspendedAt,
    DateTime? ArchivedAt, DateTime CreatedAt, DateTime? UpdatedAt);
