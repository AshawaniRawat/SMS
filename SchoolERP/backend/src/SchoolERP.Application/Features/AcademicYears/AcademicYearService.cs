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

namespace SchoolERP.Application.Features.AcademicYears;

public sealed class AcademicYearService(IApplicationDbContext context, ICurrentTenant currentTenant,
    IValidator<CreateAcademicYearRequest> createValidator, IValidator<UpdateAcademicYearRequest> updateValidator,
    IValidator<OpenAdmissionsRequest> admissionsValidator, IResourceScopeValidator scope, ICurrentSchoolResolver currentSchool, TimeProvider timeProvider) : IAcademicYearService
{
    public async Task<AcademicYearDto> CreateAsync(CreateAcademicYearRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct); var tenantId = RequireTenant();
        var tenantStatus = await TenantStatusFor(tenantId, ct); if (tenantStatus == TenantStatus.Archived) throw new ParentNotOperationalException("Academic years cannot be created for an archived tenant.");
        var schoolId=await currentSchool.ResolveAsync(ct); var school = await context.Schools.SingleOrDefaultAsync(x => x.Id == schoolId, ct);
        if (school is null || !scope.CanAccessSchool(school.Id)) throw new ResourceNotFoundException("School");
        if (school.Status is SchoolStatus.Suspended or SchoolStatus.Archived) throw new ParentNotOperationalException("Academic years cannot be created under a suspended or archived school.");
        var code = AcademicYear.NormalizeCode(request.Code); await EnsureUniqueAndNonOverlapping(school.Id, code, request.StartDate, request.EndDate, null, ct);
        var year = AcademicYear.Create(school, request.Name, code, request.StartDate, request.EndDate, request.Description, Now());
        year.UpdateDetails(request.Name, request.StartDate, request.EndDate, request.Description, request.AdmissionStartDate, request.AdmissionEndDate, request.LockDate);
        context.AcademicYears.Add(year); await context.SaveChangesAsync(ct); return Map(year);
    }
    public async Task<AcademicYearDetailsDto> CreateAcademicYearAsync(CreateAcademicYearRequest request,CancellationToken ct=default)=>Details(await CreateAsync(request,ct));
    public async Task<AcademicYearDetailsDto?> GetAcademicYearAsync(Guid id,CancellationToken ct=default){var schoolId=await currentSchool.ResolveAsync(ct);var x=await ScopedYears().Where(x=>x.Id==id&&x.SchoolId==schoolId).Select(Project()).SingleOrDefaultAsync(ct);return x is null?null:Details(x);}
    public async Task<PagedResult<AcademicYearListItemDto>> GetAcademicYearsAsync(GetAcademicYearsQuery r,CancellationToken ct=default){var schoolId=await currentSchool.ResolveAsync(ct);var page=new PagedRequest(r.PageNumber,r.PageSize);var q=ScopedYears().Where(x=>x.SchoolId==schoolId).WhereIf(r.Status.HasValue,x=>x.Status==r.Status).WhereIf(r.IsCurrent.HasValue,x=>x.IsCurrent==r.IsCurrent).WhereIf(r.IsAdmissionOpen.HasValue,x=>x.IsAdmissionOpen==r.IsAdmissionOpen);if(!string.IsNullOrWhiteSpace(r.Search)){var t=r.Search.Trim().ToLowerInvariant();q=q.Where(x=>x.Name.ToLower().Contains(t)||x.Code.ToLower().Contains(t)||(x.Description!=null&&x.Description.ToLower().Contains(t)));}var desc=string.Equals(r.SortDirection,"desc",StringComparison.OrdinalIgnoreCase);q=(r.SortBy?.ToLowerInvariant(),desc) switch{("enddate",false)=>q.OrderBy(x=>x.EndDate),("enddate",true)=>q.OrderByDescending(x=>x.EndDate),("name",false)=>q.OrderBy(x=>x.Name),("name",true)=>q.OrderByDescending(x=>x.Name),("code",false)=>q.OrderBy(x=>x.Code),("code",true)=>q.OrderByDescending(x=>x.Code),("status",false)=>q.OrderBy(x=>x.Status),("status",true)=>q.OrderByDescending(x=>x.Status),("createdat",false)=>q.OrderBy(x=>x.CreatedAt),("createdat",true)=>q.OrderByDescending(x=>x.CreatedAt),("startdate",false)=>q.OrderBy(x=>x.StartDate),_=>q.OrderByDescending(x=>x.StartDate)};return await q.Select(x=>new AcademicYearListItemDto(x.Id,x.Name,x.Code,x.StartDate,x.EndDate,x.Status,x.IsCurrent,x.IsAdmissionOpen,x.AdmissionStartDate,x.AdmissionEndDate,x.LockDate,x.CreatedAt,x.UpdatedAt)).ToPagedResultAsync(page,ct);}

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
        RequireTenant(); var schoolId=await currentSchool.ResolveAsync(ct); var info = await ScopedYears().Where(x => x.Id == id&&x.SchoolId==schoolId).Select(x => new { x.Id, x.SchoolId }).SingleOrDefaultAsync(ct); if (info is null) return null;
        var current = await context.AcademicYears.SingleOrDefaultAsync(x => x.SchoolId == info.SchoolId && x.IsCurrent && x.Id != id, ct);
        var target = await context.AcademicYears.SingleAsync(x => x.Id == id && (!scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId), ct);
        current?.UnmarkAsCurrent(); target.MarkAsCurrent(Now()); await context.SaveChangesAsync(ct); return Map(target);
    }

    public Task<AcademicYearDto?> CloseAsync(Guid id, CancellationToken ct = default) => Change(id, x => x.Close(Now()), ct);
    public Task<AcademicYearDto?> ArchiveAsync(Guid id, CancellationToken ct = default) => Change(id, x => x.Archive(Now()), ct);
    public async Task<AcademicYearDto?> OpenAdmissionsAsync(Guid id, OpenAdmissionsRequest request, CancellationToken ct = default) { await admissionsValidator.ValidateAndThrowAsync(request, ct); return await Change(id, x => x.OpenAdmissions(request.StartDate, request.EndDate, Now()), ct); }
    public Task<AcademicYearDto?> CloseAdmissionsAsync(Guid id, CancellationToken ct = default) => Change(id, x => x.CloseAdmissions(Now()), ct);
    public async Task<AcademicYearDto?> SetLockDateAsync(Guid id,SetAcademicYearLockDateRequest request,CancellationToken ct=default){var y=await FindForWrite(id,ct);if(y is null)return null;if(y.Status is AcademicYearStatus.Closed or AcademicYearStatus.Archived)throw new SchoolERP.Domain.Exceptions.DomainException("Closed or archived academic years are read-only.");y.SetLockDate(request.LockDate);await context.SaveChangesAsync(ct);return Map(y);}
    private async Task<AcademicYearDto?> Change(Guid id, Action<AcademicYear> action, CancellationToken ct) { RequireTenant(); var year = await FindForWrite(id, ct); if (year is null) return null; action(year); await context.SaveChangesAsync(ct); return Map(year); }

    private async Task<AcademicYear?> FindForWrite(Guid id, CancellationToken ct){var schoolId=await currentSchool.ResolveAsync(ct);return await context.AcademicYears.SingleOrDefaultAsync(x => x.Id == id&&x.SchoolId==schoolId&&(!scope.SchoolId.HasValue||x.SchoolId==scope.SchoolId),ct);}
    private IQueryable<AcademicYear> ScopedYears() => context.AcademicYears.AsNoTracking().Where(x => !scope.SchoolId.HasValue || x.SchoolId == scope.SchoolId);
    private async Task<bool> IsVisibleSchool(Guid schoolId, CancellationToken ct) => scope.CanAccessSchool(schoolId) && await context.Schools.AsNoTracking().AnyAsync(x => x.Id == schoolId, ct);

    private async Task EnsureUniqueAndNonOverlapping(Guid schoolId, string code, DateOnly start, DateOnly end, Guid? excludedId, CancellationToken ct)
    { if (await context.AcademicYears.AsNoTracking().AnyAsync(x => x.SchoolId == schoolId && x.Code == code && (!excludedId.HasValue || x.Id != excludedId), ct)) throw new AcademicYearCodeConflictException(); if (await HasOverlap(schoolId, start, end, excludedId, ct)) throw new AcademicYearOverlapException(); }
    private Task<bool> HasOverlap(Guid schoolId, DateOnly start, DateOnly end, Guid? excludedId, CancellationToken ct) => context.AcademicYears.AsNoTracking().AnyAsync(x => x.SchoolId == schoolId && x.StartDate <= end && x.EndDate >= start && (!excludedId.HasValue || x.Id != excludedId), ct);
    private Guid RequireTenant() => currentTenant is { IsAvailable: true, TenantId: { } id } && id != Guid.Empty ? id : throw new TenantContextMissingException();
    private async Task<TenantStatus?> TenantStatusFor(Guid id, CancellationToken ct) => await context.Tenants.AsNoTracking().Where(x => x.Id == id).Select(x => (TenantStatus?)x.Status).SingleOrDefaultAsync(ct) ?? throw new TenantMismatchException();
    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
    private static AcademicYearDto Map(AcademicYear x) => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.StartDate, x.EndDate, x.Status, x.IsCurrent, x.IsAdmissionOpen, x.AdmissionStartDate, x.AdmissionEndDate, x.LockDate, x.Description, x.CreatedAt, x.UpdatedAt);
    private static AcademicYearDetailsDto Details(AcademicYearDto x)=>new(x.Id,x.Name,x.Code,x.StartDate,x.EndDate,x.Status,x.IsCurrent,x.IsAdmissionOpen,x.AdmissionStartDate,x.AdmissionEndDate,x.LockDate,x.Description,x.CreatedAt,x.UpdatedAt);
    private static Expression<Func<AcademicYear, AcademicYearDto>> Project() => x => new(x.Id, x.TenantId, x.SchoolId, x.Name, x.Code, x.StartDate, x.EndDate, x.Status, x.IsCurrent, x.IsAdmissionOpen, x.AdmissionStartDate, x.AdmissionEndDate, x.LockDate, x.Description, x.CreatedAt, x.UpdatedAt);
}
