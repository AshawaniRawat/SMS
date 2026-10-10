using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Extensions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Schools;

public sealed class SchoolService(
    IApplicationDbContext context,
    ICurrentTenant currentTenant,
    IValidator<CreateSchoolRequest> createValidator,
    IValidator<SchoolProfileRequest> profileValidator,
    TimeProvider timeProvider) : ISchoolService
{
    public async Task<SchoolDto> CreateAsync(CreateSchoolRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var tenantId = RequireTenant();
        if (request.TenantId != tenantId) throw new TenantMismatchException();
        await EnsureOperationalTenantAsync(tenantId, cancellationToken);
        var code = School.NormalizeCode(request.Code);
        if (await context.Schools.AsNoTracking().AnyAsync(x => x.Code == code, cancellationToken))
            throw new SchoolCodeConflictException();

        var school = School.Create(tenantId, request.Name, code, occurredOnUtc: Now());
        ApplyProfile(school, request);
        context.Schools.Add(school);
        await context.SaveChangesAsync(cancellationToken);
        return Map(school);
    }

    public async Task<SchoolDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RequireTenant();
        return await context.Schools.AsNoTracking().Where(x => x.Id == id).Select(Project()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SchoolDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        RequireTenant();
        var normalized = School.NormalizeCode(code);
        return await context.Schools.AsNoTracking().Where(x => x.Code == normalized).Select(Project()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PagedResult<SchoolDto>> ListAsync(PagedRequest page, string? search, SchoolStatus? status,
        SchoolBoardType? boardType, CancellationToken cancellationToken = default)
    {
        RequireTenant();
        var query = context.Schools.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term) ||
                (x.ShortName != null && x.ShortName.ToLower().Contains(term)) ||
                (x.Email != null && x.Email.Contains(term)));
        }
        query = query.WhereIf(status.HasValue, x => x.Status == status)
            .WhereIf(boardType.HasValue, x => x.BoardType == boardType);
        return query.OrderBy(x => x.Name).Select(Project()).ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<SchoolDto?> UpdateProfileAsync(Guid id, SchoolProfileRequest request, CancellationToken cancellationToken = default)
    {
        await profileValidator.ValidateAndThrowAsync(request, cancellationToken);
        var tenantId = RequireTenant();
        await EnsureOperationalTenantAsync(tenantId, cancellationToken);
        var school = await context.Schools.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (school is null) return null;
        ApplyProfile(school, request);
        await context.SaveChangesAsync(cancellationToken);
        return Map(school);
    }

    public Task<SchoolDto?> ActivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Activate(Now()), cancellationToken);
    public Task<SchoolDto?> SuspendAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Suspend(Now()), cancellationToken);
    public Task<SchoolDto?> ReactivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Reactivate(Now()), cancellationToken);
    public Task<SchoolDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, x => x.Archive(Now()), cancellationToken);

    private async Task<SchoolDto?> ChangeStatusAsync(Guid id, Action<School> change, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenant();
        await EnsureOperationalTenantAsync(tenantId, cancellationToken);
        var school = await context.Schools.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (school is null) return null;
        change(school);
        await context.SaveChangesAsync(cancellationToken);
        return Map(school);
    }

    private Guid RequireTenant() => currentTenant is { IsAvailable: true, TenantId: { } id } && id != Guid.Empty
        ? id : throw new TenantContextMissingException();

    private async Task EnsureOperationalTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var status = await context.Tenants.AsNoTracking().Where(x => x.Id == tenantId)
            .Select(x => (TenantStatus?)x.Status).SingleOrDefaultAsync(cancellationToken);
        if (status is null) throw new TenantMismatchException();
        if (status is TenantStatus.Suspended or TenantStatus.Archived) throw new TenantNotOperationalException();
    }

    private static void ApplyProfile(School school, SchoolProfileRequest request) => school.UpdateProfile(
        request.Name, request.ShortName, request.RegistrationNumber, request.AffiliationNumber,
        request.BoardCode, request.BoardType, request.Email, request.Phone, request.AlternatePhone,
        request.Website, request.AddressLine1, request.AddressLine2, request.City, request.State,
        request.PostalCode, request.CountryCode, request.LogoUrl, request.TimeZoneId,
        request.DefaultCurrency, request.PrincipalName);

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;

    private static SchoolDto Map(School x) => new(x.Id, x.TenantId, x.Name, x.Code, x.ShortName,
        x.RegistrationNumber, x.AffiliationNumber, x.BoardCode, x.BoardType, x.Email, x.Phone,
        x.AlternatePhone, x.Website, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode,
        x.CountryCode, x.LogoUrl, x.TimeZoneId, x.DefaultCurrency, x.PrincipalName, x.Status,
        x.ActivatedAt, x.SuspendedAt, x.ArchivedAt, x.CreatedAt, x.UpdatedAt);

    private static Expression<Func<School, SchoolDto>> Project() => x => new(x.Id, x.TenantId,
        x.Name, x.Code, x.ShortName, x.RegistrationNumber, x.AffiliationNumber, x.BoardCode,
        x.BoardType, x.Email, x.Phone, x.AlternatePhone, x.Website, x.AddressLine1, x.AddressLine2,
        x.City, x.State, x.PostalCode, x.CountryCode, x.LogoUrl, x.TimeZoneId, x.DefaultCurrency,
        x.PrincipalName, x.Status, x.ActivatedAt, x.SuspendedAt, x.ArchivedAt, x.CreatedAt, x.UpdatedAt);
}
