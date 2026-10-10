using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Extensions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Tenants;

public sealed class TenantService(
    IApplicationDbContext context,
    IValidator<CreateTenantRequest> validator,
    TimeProvider timeProvider) : ITenantService
{
    public async Task<TenantDto> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var slug = Tenant.NormalizeSlug(request.Slug);
        if (await context.Tenants.AsNoTracking().AnyAsync(tenant => tenant.Slug == slug, cancellationToken))
            throw new TenantSlugConflictException();

        var tenant = Tenant.Create(request.Name, slug, request.Email, request.Phone, request.CountryCode, request.TimeZoneId,
            timeProvider.GetUtcNow().UtcDateTime);
        tenant.UpdateProfile(tenant.Name, null, tenant.Email, tenant.Phone, null, null, tenant.CountryCode, tenant.TimeZoneId, request.DefaultCurrency);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);
        return Map(tenant);
    }

    public Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Tenants.AsNoTracking().Where(tenant => tenant.Id == id).Select(Project()).SingleOrDefaultAsync(cancellationToken);

    public Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = Tenant.NormalizeSlug(slug);
        return context.Tenants.AsNoTracking().Where(tenant => tenant.Slug == normalized).Select(Project()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PagedResult<TenantDto>> ListAsync(
        PagedRequest page, string? search, TenantStatus? status, CancellationToken cancellationToken = default)
    {
        var query = context.Tenants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(tenant => tenant.Name.ToLower().Contains(term) || tenant.Slug.Contains(term) ||
                (tenant.Email != null && tenant.Email.Contains(term)));
        }
        query = query.WhereIf(status.HasValue, tenant => tenant.Status == status);
        return query.OrderBy(tenant => tenant.Name).Select(Project()).ToPagedResultAsync(page, cancellationToken);
    }

    public Task<TenantDto?> ActivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, tenant => tenant.Activate(Now()), cancellationToken);
    public Task<TenantDto?> SuspendAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, tenant => tenant.Suspend(Now()), cancellationToken);
    public Task<TenantDto?> ReactivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, tenant => tenant.Reactivate(Now()), cancellationToken);
    public Task<TenantDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, tenant => tenant.Archive(Now()), cancellationToken);

    private async Task<TenantDto?> ChangeStatusAsync(Guid id, Action<Tenant> change, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (tenant is null) return null;
        change(tenant);
        await context.SaveChangesAsync(cancellationToken);
        return Map(tenant);
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private static TenantDto Map(Tenant tenant) => new(tenant.Id, tenant.Name, tenant.Slug, tenant.Email, tenant.Phone,
        tenant.CountryCode, tenant.TimeZoneId, tenant.DefaultCurrency, tenant.Status, tenant.CreatedAt);
    private static System.Linq.Expressions.Expression<Func<Tenant, TenantDto>> Project() => tenant =>
        new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Email, tenant.Phone, tenant.CountryCode,
            tenant.TimeZoneId, tenant.DefaultCurrency, tenant.Status, tenant.CreatedAt);
}
