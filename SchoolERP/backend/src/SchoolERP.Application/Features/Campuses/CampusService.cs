using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Extensions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Campuses;

public sealed class CampusService(IApplicationDbContext context, ICurrentTenant currentTenant,
    IValidator<CreateCampusRequest> createValidator, IValidator<CampusProfileRequest> profileValidator,
    IValidator<ChangeCampusCodeRequest> codeValidator, TimeProvider timeProvider) : ICampusService
{
    public async Task<CampusDto> CreateAsync(CreateCampusRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct); var tenantId = RequireTenant();
        await EnsureTenantOperational(tenantId, ct);
        var school = await context.Schools.SingleOrDefaultAsync(x => x.Id == request.SchoolId, ct);
        if (school is null) throw new TenantMismatchException();
        if (school.Status is SchoolStatus.Suspended or SchoolStatus.Archived)
            throw new ParentNotOperationalException("A campus cannot be created under a suspended or archived school.");
        var code = Campus.NormalizeCode(request.Code);
        if (await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == school.Id && x.Code == code, ct)) throw new CampusCodeConflictException();
        var hasCampus = await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == school.Id, ct);
        var main = request.IsMainCampus ?? !hasCampus;
        if (main && await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == school.Id && x.IsMainCampus, ct)) throw new MainCampusConflictException();
        var campus = Campus.Create(school, request.Name, code, main, Now()); Apply(campus, request);
        context.Campuses.Add(campus); await context.SaveChangesAsync(ct); return Map(campus);
    }

    public async Task<CampusDto?> GetByIdAsync(Guid id, CancellationToken ct = default) { RequireTenant(); return await context.Campuses.AsNoTracking().Where(x => x.Id == id).Select(Project()).SingleOrDefaultAsync(ct); }
    public async Task<CampusDto?> GetByCodeAsync(Guid schoolId, string code, CancellationToken ct = default) { RequireTenant(); var normalized = Campus.NormalizeCode(code); return await context.Campuses.AsNoTracking().Where(x => x.SchoolId == schoolId && x.Code == normalized).Select(Project()).SingleOrDefaultAsync(ct); }
    public Task<PagedResult<CampusDto>> ListAsync(PagedRequest page, Guid? schoolId, string? search, CampusStatus? status, bool? isMain, CancellationToken ct = default)
    {
        RequireTenant(); var query = context.Campuses.AsNoTracking();
        query = query.WhereIf(schoolId.HasValue, x => x.SchoolId == schoolId).WhereIf(status.HasValue, x => x.Status == status).WhereIf(isMain.HasValue, x => x.IsMainCampus == isMain);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLowerInvariant(); query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term) || (x.ShortName != null && x.ShortName.ToLower().Contains(term)) || (x.City != null && x.City.ToLower().Contains(term))); }
        return query.OrderBy(x => x.Name).Select(Project()).ToPagedResultAsync(page, ct);
    }

    public async Task<CampusDto?> UpdateAsync(Guid id, CampusProfileRequest request, CancellationToken ct = default)
    { await profileValidator.ValidateAndThrowAsync(request, ct); var campus = await FindForWrite(id, ct); if (campus is null) return null; Apply(campus, request); await context.SaveChangesAsync(ct); return Map(campus); }
    public async Task<CampusDto?> ChangeCodeAsync(Guid id, ChangeCampusCodeRequest request, CancellationToken ct = default)
    { await codeValidator.ValidateAndThrowAsync(request, ct); var campus = await FindForWrite(id, ct); if (campus is null) return null; var code = Campus.NormalizeCode(request.Code); if (await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == campus.SchoolId && x.Code == code && x.Id != id, ct)) throw new CampusCodeConflictException(); campus.ChangeCode(code); await context.SaveChangesAsync(ct); return Map(campus); }
    public async Task<CampusDto?> SetMainAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant(); await EnsureTenantOperational(tenantId, ct);
        var targetInfo = await context.Campuses.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.SchoolId }).SingleOrDefaultAsync(ct);
        if (targetInfo is null) return null;
        // Track the current main first so EF updates it before the replacement inside its SaveChanges transaction.
        var current = await context.Campuses.SingleOrDefaultAsync(x => x.SchoolId == targetInfo.SchoolId && x.IsMainCampus && x.Id != id, ct);
        var target = await context.Campuses.SingleAsync(x => x.Id == id, ct);
        current?.RemoveMainCampusDesignation(); target.MarkAsMainCampus(Now());
        await context.SaveChangesAsync(ct); return Map(target);
    }

    public Task<CampusDto?> ActivateAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, true, x => x.Activate(Now()), ct);
    public Task<CampusDto?> SuspendAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, false, x => x.Suspend(Now()), ct);
    public Task<CampusDto?> ReactivateAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, true, x => x.Reactivate(Now()), ct);
    public Task<CampusDto?> ArchiveAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, false, x => x.Archive(Now()), ct);
    private async Task<CampusDto?> ChangeStatus(Guid id, bool requiresActiveSchool, Action<Campus> action, CancellationToken ct)
    { var campus = await FindForWrite(id, ct); if (campus is null) return null; if (requiresActiveSchool) { var status = await context.Schools.AsNoTracking().Where(x => x.Id == campus.SchoolId).Select(x => x.Status).SingleAsync(ct); if (status != SchoolStatus.Active) throw new ParentNotOperationalException("Campus activation requires an active school."); } action(campus); await context.SaveChangesAsync(ct); return Map(campus); }

    private async Task<Campus?> FindForWrite(Guid id, CancellationToken ct) { var tenantId = RequireTenant(); await EnsureTenantOperational(tenantId, ct); return await context.Campuses.SingleOrDefaultAsync(x => x.Id == id, ct); }
    private Guid RequireTenant() => currentTenant is { IsAvailable: true, TenantId: { } id } && id != Guid.Empty ? id : throw new TenantContextMissingException();
    private async Task EnsureTenantOperational(Guid id, CancellationToken ct) { var status = await context.Tenants.AsNoTracking().Where(x => x.Id == id).Select(x => (TenantStatus?)x.Status).SingleOrDefaultAsync(ct); if (status is null) throw new TenantMismatchException(); if (status is TenantStatus.Suspended or TenantStatus.Archived) throw new ParentNotOperationalException("Campus operations are blocked for a suspended or archived tenant."); }
    private static void Apply(Campus x, CampusProfileRequest r) => x.UpdateProfile(r.Name, r.ShortName, r.Email, r.Phone, r.AlternatePhone, r.AddressLine1, r.AddressLine2, r.City, r.State, r.PostalCode, r.CountryCode, r.TimeZoneId, r.Latitude, r.Longitude);
    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private static CampusDto Map(Campus x) => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.ShortName, x.Email, x.Phone, x.AlternatePhone, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.CountryCode, x.TimeZoneId, x.Latitude, x.Longitude, x.IsMainCampus, x.Status, x.ActivatedAt, x.SuspendedAt, x.ArchivedAt, x.CreatedAt, x.UpdatedAt);
    private static Expression<Func<Campus, CampusDto>> Project() => x => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.ShortName, x.Email, x.Phone, x.AlternatePhone, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.CountryCode, x.TimeZoneId, x.Latitude, x.Longitude, x.IsMainCampus, x.Status, x.ActivatedAt, x.SuspendedAt, x.ArchivedAt, x.CreatedAt, x.UpdatedAt);
}
