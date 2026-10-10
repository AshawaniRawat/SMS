using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Tenants;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string? Email,
    string? Phone,
    string? CountryCode,
    string? TimeZoneId,
    string? DefaultCurrency,
    TenantStatus Status,
    DateTime CreatedAt);
