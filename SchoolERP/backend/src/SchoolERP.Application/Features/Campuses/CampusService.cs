using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Extensions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Application.Security;
using SchoolERP.Application.Features.Schools;

namespace SchoolERP.Application.Features.Campuses;

public sealed class CampusService(IApplicationDbContext context, ICurrentTenant currentTenant,
    IValidator<CreateCampusRequest> createValidator, IValidator<CampusProfileRequest> profileValidator,
    IValidator<ChangeCampusCodeRequest> codeValidator, IResourceScopeValidator scope,
    ICurrentSchoolResolver currentSchool, TimeProvider timeProvider) : ICampusService
{
    public async Task<CampusDto> CreateAsync(CreateCampusRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct); var tenantId = RequireTenant();
        await EnsureTenantOperational(tenantId, ct);
        if (scope.CampusId.HasValue) throw new UnauthorizedAccessException("Campus-scoped users cannot create sibling campuses.");
        var schoolId = await currentSchool.ResolveAsync(ct);
        var school = await context.Schools.SingleOrDefaultAsync(x => x.Id == schoolId, ct);
        if (school is null || !scope.CanAccessSchool(school.Id)) throw new ResourceNotFoundException("School");
        if (school.Status is SchoolStatus.Suspended or SchoolStatus.Archived)
            throw new ParentNotOperationalException("A campus cannot be created under a suspended or archived school.");
        var code = Campus.NormalizeCode(request.Code);
        if (await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == school.Id && x.Code == code, ct)) throw new CampusCodeConflictException();
        var hasCampus = await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == school.Id, ct);
        var main = request.IsMainCampus || !hasCampus;
        if (main)
        {
            var previousMain = await context.Campuses.SingleOrDefaultAsync(x => x.SchoolId == school.Id && x.IsMainCampus, ct);
            previousMain?.RemoveMainCampusDesignation();
        }
        var campus = Campus.Create(school, request.Name, code, main, Now()); Apply(campus, request);
        context.Campuses.Add(campus); await context.SaveChangesAsync(ct); return Map(campus);
    }

    public async Task<CampusDetailsDto> CreateCampusAsync(CreateCampusRequest request, CancellationToken ct = default) =>
        Details(await CreateAsync(request, ct));

    public async Task<CampusDetailsDto?> GetCampusAsync(Guid id, CancellationToken ct = default)
    {
        var schoolId = await currentSchool.ResolveAsync(ct);
        var campus = await ScopedCampuses().Where(x => x.Id == id && x.SchoolId == schoolId)
            .Select(Project()).SingleOrDefaultAsync(ct);
        return campus is null ? null : Details(campus);
    }

    public async Task<PagedResult<CampusListItemDto>> GetCampusesAsync(GetCampusesQuery request, CancellationToken ct = default)
    {
        var schoolId = await currentSchool.ResolveAsync(ct);
        var page = new PagedRequest(request.PageNumber, request.PageSize);
        var query = ScopedCampuses().Where(x => x.SchoolId == schoolId)
            .WhereIf(request.Status.HasValue, x => x.Status == request.Status)
            .WhereIf(request.IsMain.HasValue, x => x.IsMainCampus == request.IsMain)
            .WhereIf(!string.IsNullOrWhiteSpace(request.City), x => x.City != null && x.City.ToLower() == request.City!.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term) ||
                x.ShortName != null && x.ShortName.ToLower().Contains(term) || x.City != null && x.City.ToLower().Contains(term));
        }
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("code", false) => query.OrderBy(x => x.Code), ("code", true) => query.OrderByDescending(x => x.Code),
            ("status", false) => query.OrderBy(x => x.Status), ("status", true) => query.OrderByDescending(x => x.Status),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt), ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            ("city", false) => query.OrderBy(x => x.City), ("city", true) => query.OrderByDescending(x => x.City),
            ("name", true) => query.OrderByDescending(x => x.Name),
            _ => query.OrderByDescending(x => x.IsMainCampus).ThenBy(x => x.Name)
        };
        return await query.Select(x => new CampusListItemDto(x.Id, x.Name, x.Code, x.ShortName, x.Status,
            x.IsMainCampus, x.City, x.State, x.CountryCode, x.Phone, x.Email, x.CreatedAt, x.UpdatedAt))
            .ToPagedResultAsync(page, ct);
    }

    public async Task<CampusDetailsDto?> UpdateCampusAsync(Guid id, CampusProfileRequest request, CancellationToken ct = default)
    {
        var item = await UpdateAsync(id, request, ct);
        return item is null ? null : Details(item);
    }

    public async Task<CampusDto?> GetByIdAsync(Guid id, CancellationToken ct = default) { RequireTenant(); return await ScopedCampuses().Where(x => x.Id == id).Select(Project()).SingleOrDefaultAsync(ct); }
    public async Task<CampusDto?> GetForSchoolAsync(Guid schoolId, Guid campusId, CancellationToken ct = default) { RequireTenant(); if (!scope.CanAccessSchool(schoolId)) return null; return await ScopedCampuses().Where(x => x.Id == campusId && x.SchoolId == schoolId).Select(Project()).SingleOrDefaultAsync(ct); }
    public async Task<CampusDto?> GetByCodeAsync(Guid schoolId, string code, CancellationToken ct = default) { RequireTenant(); if (!await IsVisibleSchool(schoolId, ct)) return null; var normalized = Campus.NormalizeCode(code); return await ScopedCampuses().Where(x => x.SchoolId == schoolId && x.Code == normalized).Select(Project()).SingleOrDefaultAsync(ct); }
    public Task<PagedResult<CampusDto>> ListAsync(PagedRequest page, Guid? schoolId, string? search, CampusStatus? status, bool? isMain, CancellationToken ct = default)
    {
        RequireTenant(); var query = ScopedCampuses();
        query = query.WhereIf(schoolId.HasValue, x => x.SchoolId == schoolId).WhereIf(status.HasValue, x => x.Status == status).WhereIf(isMain.HasValue, x => x.IsMainCampus == isMain);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLowerInvariant(); query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term) || (x.ShortName != null && x.ShortName.ToLower().Contains(term)) || (x.City != null && x.City.ToLower().Contains(term))); }
        return query.OrderBy(x => x.Name).Select(Project()).ToPagedResultAsync(page, ct);
    }

    public async Task<PagedResult<CampusDto>?> ListForSchoolAsync(PagedRequest page, Guid schoolId, string? search, CampusStatus? status, bool? isMain, CancellationToken ct = default)
    { if (!await IsVisibleSchool(schoolId, ct)) return null; return await ListAsync(page, schoolId, search, status, isMain, ct); }

    public async Task<CampusDto?> UpdateAsync(Guid id, CampusProfileRequest request, CancellationToken ct = default)
    { await profileValidator.ValidateAndThrowAsync(request, ct); var campus = await FindForWrite(id, ct); if (campus is null) return null; EnsureWritable(campus); Apply(campus, request); campus.RecordProfileUpdated(Now()); await context.SaveChangesAsync(ct); return Map(campus); }
    public async Task<CampusDto?> ChangeCodeAsync(Guid id, ChangeCampusCodeRequest request, CancellationToken ct = default)
    { await codeValidator.ValidateAndThrowAsync(request, ct); var campus = await FindForWrite(id, ct); if (campus is null) return null; EnsureWritable(campus); var code = Campus.NormalizeCode(request.Code); if (await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == campus.SchoolId && x.Code == code && x.Id != id, ct)) throw new CampusCodeConflictException(); campus.ChangeCode(code, Now()); await context.SaveChangesAsync(ct); return Map(campus); }
    public async Task<CampusDto?> SetMainAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant(); await EnsureTenantOperational(tenantId, ct);
        var schoolId = await currentSchool.ResolveAsync(ct);
        var targetInfo = await ScopedCampuses().Where(x => x.Id == id && x.SchoolId == schoolId)
            .Select(x => new { x.Id, x.SchoolId }).SingleOrDefaultAsync(ct);
        if (targetInfo is null) return null;
        var targetStatus = await context.Campuses.AsNoTracking().Where(x => x.Id == id).Select(x => x.Status).SingleAsync(ct);
        if (targetStatus == CampusStatus.Archived) throw new CampusArchivedReadOnlyException();
        // Track the current main first so EF updates it before the replacement inside its SaveChanges transaction.
        var current = await context.Campuses.SingleOrDefaultAsync(x => x.SchoolId == targetInfo.SchoolId && x.IsMainCampus && x.Id != id, ct);
        var target = await context.Campuses.SingleAsync(x => x.Id == id &&
            (!scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId) &&
            (!scope.CampusId.HasValue || x.Id == scope.CampusId), ct);
        current?.RemoveMainCampusDesignation(); target.MarkAsMainCampus(Now());
        await context.SaveChangesAsync(ct); return Map(target);
    }

    public Task<CampusDto?> ActivateAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, true, false, false, x => x.Activate(Now()), ct);
    public Task<CampusDto?> SuspendAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, false, true, false, x => x.Suspend(Now()), ct);
    public Task<CampusDto?> ReactivateAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, true, false, false, x => x.Reactivate(Now()), ct);
    public Task<CampusDto?> ArchiveAsync(Guid id, CancellationToken ct = default) => ChangeStatus(id, false, false, true, x => x.Archive(Now()), ct);
    private async Task<CampusDto?> ChangeStatus(Guid id, bool requiresActiveSchool, bool suspending, bool archiving, Action<Campus> action, CancellationToken ct)
    { var campus = await FindForWrite(id, ct); if (campus is null) return null; EnsureWritable(campus); if (requiresActiveSchool) { var status = await context.Schools.AsNoTracking().Where(x => x.Id == campus.SchoolId).Select(x => x.Status).SingleAsync(ct); if (status != SchoolStatus.Active) throw new ParentNotOperationalException("Campus activation requires an active school."); } var hasAlternative = campus.IsMainCampus && await context.Campuses.AsNoTracking().AnyAsync(x => x.SchoolId == campus.SchoolId && x.Id != id && x.Status != CampusStatus.Archived, ct); if (suspending && campus.Status == CampusStatus.Active && hasAlternative) throw new MainCampusCannotBeSuspendedException(); if (archiving && hasAlternative) throw new MainCampusCannotBeArchivedException(); action(campus); await context.SaveChangesAsync(ct); return Map(campus); }

    private async Task<Campus?> FindForWrite(Guid id, CancellationToken ct) { var tenantId = RequireTenant(); await EnsureTenantOperational(tenantId, ct); var schoolId = await currentSchool.ResolveAsync(ct); return await context.Campuses.SingleOrDefaultAsync(x => x.Id == id && x.SchoolId == schoolId && (!scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId) && (!scope.CampusId.HasValue || x.Id == scope.CampusId), ct); }
    private IQueryable<Campus> ScopedCampuses() => context.Campuses.AsNoTracking().Where(x =>
        (!scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId) && (!scope.CampusId.HasValue || x.Id == scope.CampusId));
    private async Task<bool> IsVisibleSchool(Guid schoolId, CancellationToken ct) => scope.CanAccessSchool(schoolId) &&
        await context.Schools.AsNoTracking().AnyAsync(x => x.Id == schoolId, ct);
    private Guid RequireTenant() => currentTenant is { IsAvailable: true, TenantId: { } id } && id != Guid.Empty ? id : throw new TenantContextMissingException();
    private async Task EnsureTenantOperational(Guid id, CancellationToken ct) { var status = await context.Tenants.AsNoTracking().Where(x => x.Id == id).Select(x => (TenantStatus?)x.Status).SingleOrDefaultAsync(ct); if (status is null) throw new TenantMismatchException(); if (status is TenantStatus.Suspended or TenantStatus.Archived) throw new ParentNotOperationalException("Campus operations are blocked for a suspended or archived tenant."); }
    private static void Apply(Campus x, CampusProfileRequest r) => x.UpdateProfile(r.Name, r.ShortName, r.Email, r.Phone, r.AlternatePhone, r.AddressLine1, r.AddressLine2, r.City, r.State, r.PostalCode, r.CountryCode, r.TimeZoneId, r.Latitude, r.Longitude);
    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private static CampusDto Map(Campus x) => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.ShortName, x.Email, x.Phone, x.AlternatePhone, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.CountryCode, x.TimeZoneId, x.Latitude, x.Longitude, x.IsMainCampus, x.Status, x.ActivatedAt, x.SuspendedAt, x.ArchivedAt, x.CreatedAt, x.UpdatedAt);
    private static CampusDetailsDto Details(CampusDto x) => new(x.Id, x.Name, x.Code, x.ShortName, x.Email, x.Phone,
        x.AlternatePhone, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.CountryCode, x.TimeZoneId,
        x.Latitude, x.Longitude, x.Status, x.IsMainCampus, x.ActivatedAt, x.SuspendedAt, x.ArchivedAt, x.CreatedAt, x.UpdatedAt);
    private static void EnsureWritable(Campus campus) { if (campus.Status == CampusStatus.Archived) throw new CampusArchivedReadOnlyException(); }
    private static Expression<Func<Campus, CampusDto>> Project() => x => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.ShortName, x.Email, x.Phone, x.AlternatePhone, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.CountryCode, x.TimeZoneId, x.Latitude, x.Longitude, x.IsMainCampus, x.Status, x.ActivatedAt, x.SuspendedAt, x.ArchivedAt, x.CreatedAt, x.UpdatedAt);
}
