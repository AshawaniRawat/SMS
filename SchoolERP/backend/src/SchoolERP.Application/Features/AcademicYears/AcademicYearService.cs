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

namespace SchoolERP.Application.Features.AcademicYears;

public sealed class AcademicYearService(IApplicationDbContext context, ICurrentTenant currentTenant,
    IValidator<CreateAcademicYearRequest> createValidator, IValidator<UpdateAcademicYearRequest> updateValidator,
    IValidator<OpenAdmissionsRequest> admissionsValidator, IResourceScopeValidator scope, TimeProvider timeProvider) : IAcademicYearService
{
    public async Task<AcademicYearDto> CreateAsync(CreateAcademicYearRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct); var tenantId = RequireTenant();
        var tenantStatus = await TenantStatusFor(tenantId, ct); if (tenantStatus == TenantStatus.Archived) throw new ParentNotOperationalException("Academic years cannot be created for an archived tenant.");
        var school = await context.Schools.SingleOrDefaultAsync(x => x.Id == request.SchoolId, ct);
        if (school is null || !scope.CanAccessSchool(school.Id)) throw new ResourceNotFoundException("School");
        if (school.Status is SchoolStatus.Suspended or SchoolStatus.Archived) throw new ParentNotOperationalException("Academic years cannot be created under a suspended or archived school.");
        var code = AcademicYear.NormalizeCode(request.Code); await EnsureUniqueAndNonOverlapping(school.Id, code, request.StartDate, request.EndDate, null, ct);
        var year = AcademicYear.Create(school, request.Name, code, request.StartDate, request.EndDate, request.Description, Now());
        year.UpdateDetails(request.Name, request.StartDate, request.EndDate, request.Description, request.AdmissionStartDate, request.AdmissionEndDate, request.LockDate);
        context.AcademicYears.Add(year); await context.SaveChangesAsync(ct); return Map(year);
    }

    public async Task<AcademicYearDto?> GetAsync(Guid id, CancellationToken ct = default) { RequireTenant(); return await ScopedYears().Where(x => x.Id == id).Select(Project()).SingleOrDefaultAsync(ct); }
    public async Task<AcademicYearDto?> GetForSchoolAsync(Guid schoolId, Guid academicYearId, CancellationToken ct = default) { RequireTenant(); if (!scope.CanAccessSchool(schoolId)) return null; return await ScopedYears().Where(x => x.Id == academicYearId && x.SchoolId == schoolId).Select(Project()).SingleOrDefaultAsync(ct); }
    public async Task<AcademicYearDto?> GetCurrentAsync(Guid schoolId, CancellationToken ct = default) { RequireTenant(); if (!await IsVisibleSchool(schoolId, ct)) return null; return await ScopedYears().Where(x => x.SchoolId == schoolId && x.IsCurrent).Select(Project()).SingleOrDefaultAsync(ct); }
    public async Task<AcademicYearDto?> GetByCodeAsync(Guid schoolId, string code, CancellationToken ct = default) { RequireTenant(); if (!await IsVisibleSchool(schoolId, ct)) return null; var normalized = AcademicYear.NormalizeCode(code); return await ScopedYears().Where(x => x.SchoolId == schoolId && x.Code == normalized).Select(Project()).SingleOrDefaultAsync(ct); }
    public Task<PagedResult<AcademicYearDto>> ListAsync(PagedRequest page, Guid? schoolId, string? search, AcademicYearStatus? status, bool? isCurrent, CancellationToken ct = default)
    {
        RequireTenant(); var query = ScopedYears().WhereIf(schoolId.HasValue, x => x.SchoolId == schoolId)
            .WhereIf(status.HasValue, x => x.Status == status).WhereIf(isCurrent.HasValue, x => x.IsCurrent == isCurrent);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLowerInvariant(); query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term)); }
        return query.OrderByDescending(x => x.StartDate).Select(Project()).ToPagedResultAsync(page, ct);
    }

    public async Task<PagedResult<AcademicYearDto>?> ListForSchoolAsync(PagedRequest page, Guid schoolId, string? search, AcademicYearStatus? status, bool? isCurrent, CancellationToken ct = default)
    { if (!await IsVisibleSchool(schoolId, ct)) return null; return await ListAsync(page, schoolId, search, status, isCurrent, ct); }

    public async Task<AcademicYearDto?> UpdateAsync(Guid id, UpdateAcademicYearRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct); RequireTenant(); var year = await FindForWrite(id, ct); if (year is null) return null;
        var code = AcademicYear.NormalizeCode(request.Code); await EnsureUniqueAndNonOverlapping(year.SchoolId, code, request.StartDate, request.EndDate, id, ct);
        year.UpdateDetails(request.Name, request.StartDate, request.EndDate, request.Description, request.AdmissionStartDate, request.AdmissionEndDate, request.LockDate);
        if (year.Code != code) year.ChangeCode(code); await context.SaveChangesAsync(ct); return Map(year);
    }

    public async Task<AcademicYearDto?> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant(); if (await TenantStatusFor(tenantId, ct) != TenantStatus.Active) throw new ParentNotOperationalException("Academic year activation requires an active tenant.");
        var year = await FindForWrite(id, ct); if (year is null) return null;
        var schoolStatus = await context.Schools.AsNoTracking().Where(x => x.Id == year.SchoolId).Select(x => x.Status).SingleAsync(ct);
        if (schoolStatus != SchoolStatus.Active) throw new ParentNotOperationalException("Academic year activation requires an active school.");
        if (await HasOverlap(year.SchoolId, year.StartDate, year.EndDate, year.Id, ct)) throw new AcademicYearOverlapException();
        year.Activate(Now()); await context.SaveChangesAsync(ct); return Map(year);
    }

    public async Task<AcademicYearDto?> SetCurrentAsync(Guid id, CancellationToken ct = default)
    {
        RequireTenant(); var info = await ScopedYears().Where(x => x.Id == id).Select(x => new { x.Id, x.SchoolId }).SingleOrDefaultAsync(ct); if (info is null) return null;
        var current = await context.AcademicYears.SingleOrDefaultAsync(x => x.SchoolId == info.SchoolId && x.IsCurrent && x.Id != id, ct);
        var target = await context.AcademicYears.SingleAsync(x => x.Id == id && (!scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId), ct);
        current?.UnmarkAsCurrent(); target.MarkAsCurrent(Now()); await context.SaveChangesAsync(ct); return Map(target);
    }

    public Task<AcademicYearDto?> CloseAsync(Guid id, CancellationToken ct = default) => Change(id, x => x.Close(Now()), ct);
    public Task<AcademicYearDto?> ArchiveAsync(Guid id, CancellationToken ct = default) => Change(id, x => x.Archive(Now()), ct);
    public async Task<AcademicYearDto?> OpenAdmissionsAsync(Guid id, OpenAdmissionsRequest request, CancellationToken ct = default) { await admissionsValidator.ValidateAndThrowAsync(request, ct); return await Change(id, x => x.OpenAdmissions(request.StartDate, request.EndDate, Now()), ct); }
    public Task<AcademicYearDto?> CloseAdmissionsAsync(Guid id, CancellationToken ct = default) => Change(id, x => x.CloseAdmissions(Now()), ct);
    private async Task<AcademicYearDto?> Change(Guid id, Action<AcademicYear> action, CancellationToken ct) { RequireTenant(); var year = await FindForWrite(id, ct); if (year is null) return null; action(year); await context.SaveChangesAsync(ct); return Map(year); }

    private Task<AcademicYear?> FindForWrite(Guid id, CancellationToken ct) => context.AcademicYears.SingleOrDefaultAsync(x => x.Id == id && (!scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId), ct);
    private IQueryable<AcademicYear> ScopedYears() => context.AcademicYears.AsNoTracking().Where(x => !scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId);
    private async Task<bool> IsVisibleSchool(Guid schoolId, CancellationToken ct) => scope.CanAccessSchool(schoolId) && await context.Schools.AsNoTracking().AnyAsync(x => x.Id == schoolId, ct);

    private async Task EnsureUniqueAndNonOverlapping(Guid schoolId, string code, DateOnly start, DateOnly end, Guid? excludedId, CancellationToken ct)
    { if (await context.AcademicYears.AsNoTracking().AnyAsync(x => x.SchoolId == schoolId && x.Code == code && (!excludedId.HasValue || x.Id != excludedId), ct)) throw new AcademicYearCodeConflictException(); if (await HasOverlap(schoolId, start, end, excludedId, ct)) throw new AcademicYearOverlapException(); }
    private Task<bool> HasOverlap(Guid schoolId, DateOnly start, DateOnly end, Guid? excludedId, CancellationToken ct) => context.AcademicYears.AsNoTracking().AnyAsync(x => x.SchoolId == schoolId && x.StartDate <= end && x.EndDate >= start && (!excludedId.HasValue || x.Id != excludedId), ct);
    private Guid RequireTenant() => currentTenant is { IsAvailable: true, TenantId: { } id } && id != Guid.Empty ? id : throw new TenantContextMissingException();
    private async Task<TenantStatus?> TenantStatusFor(Guid id, CancellationToken ct) => await context.Tenants.AsNoTracking().Where(x => x.Id == id).Select(x => (TenantStatus?)x.Status).SingleOrDefaultAsync(ct) ?? throw new TenantMismatchException();
    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private static AcademicYearDto Map(AcademicYear x) => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.StartDate, x.EndDate, x.Status, x.IsCurrent, x.IsAdmissionOpen, x.AdmissionStartDate, x.AdmissionEndDate, x.LockDate, x.Description, x.CreatedAt, x.UpdatedAt);
    private static Expression<Func<AcademicYear, AcademicYearDto>> Project() => x => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.StartDate, x.EndDate, x.Status, x.IsCurrent, x.IsAdmissionOpen, x.AdmissionStartDate, x.AdmissionEndDate, x.LockDate, x.Description, x.CreatedAt, x.UpdatedAt);
}
