namespace SchoolERP.Application.Features.Tenants;

public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string? Email = null,
    string? Phone = null,
    string? CountryCode = null,
    string? TimeZoneId = null,
    string? DefaultCurrency = null);
