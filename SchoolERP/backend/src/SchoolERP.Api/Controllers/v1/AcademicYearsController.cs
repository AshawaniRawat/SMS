using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.AcademicYears;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;
using SchoolERP.Api.Security;
using SchoolERP.Domain.Security;

namespace SchoolERP.Api.Controllers.v1;

[Route("api/v1/academic-years")]
public sealed class AcademicYearsController(IAcademicYearService service) : ControllerBase
{
    [HttpPost, RequirePermission(PermissionCatalog.AcademicYear.Create)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Create(CreateAcademicYearRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<AcademicYearDto>.Ok(item)); }
    [HttpGet, RequirePermission(PermissionCatalog.AcademicYear.View)] public async Task<ActionResult<ApiResponse<PagedResult<AcademicYearDto>>>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? schoolId = null, [FromQuery] string? search = null, [FromQuery] AcademicYearStatus? status = null, [FromQuery] bool? isCurrent = null, CancellationToken ct = default) => Ok(ApiResponse<PagedResult<AcademicYearDto>>.Ok(await service.ListAsync(new(pageNumber, pageSize), schoolId, search, status, isCurrent, ct)));
    [HttpGet("{id:guid}"), RequirePermission(PermissionCatalog.AcademicYear.View)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Get(Guid id, CancellationToken ct) => Result(await service.GetAsync(id, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/academic-years"), RequirePermission(PermissionCatalog.AcademicYear.View)] public async Task<ActionResult<ApiResponse<PagedResult<AcademicYearDto>>>> BySchool(Guid schoolId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] AcademicYearStatus? status = null, [FromQuery] bool? isCurrent = null, CancellationToken ct = default) => Result(await service.ListForSchoolAsync(new(pageNumber, pageSize), schoolId, search, status, isCurrent, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/academic-years/{academicYearId:guid}"), RequirePermission(PermissionCatalog.AcademicYear.View)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> ForSchool(Guid schoolId, Guid academicYearId, CancellationToken ct) => Result(await service.GetForSchoolAsync(schoolId, academicYearId, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/academic-years/current"), RequirePermission(PermissionCatalog.AcademicYear.View)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Current(Guid schoolId, CancellationToken ct) => Result(await service.GetCurrentAsync(schoolId, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/academic-years/by-code/{code}"), RequirePermission(PermissionCatalog.AcademicYear.View)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> ByCode(Guid schoolId, string code, CancellationToken ct) => Result(await service.GetByCodeAsync(schoolId, code, ct));
    [HttpPut("{id:guid}"), RequirePermission(PermissionCatalog.AcademicYear.Edit)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Update(Guid id, UpdateAcademicYearRequest request, CancellationToken ct) => Result(await service.UpdateAsync(id, request, ct));
    [HttpPost("{id:guid}/activate"), RequirePermission(PermissionCatalog.AcademicYear.Activate)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Activate(Guid id, CancellationToken ct) => Result(await service.ActivateAsync(id, ct));
    [HttpPost("{id:guid}/set-current"), RequirePermission(PermissionCatalog.AcademicYear.SetCurrent)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> SetCurrent(Guid id, CancellationToken ct) => Result(await service.SetCurrentAsync(id, ct));
    [HttpPost("{id:guid}/close"), RequirePermission(PermissionCatalog.AcademicYear.Close)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Close(Guid id, CancellationToken ct) => Result(await service.CloseAsync(id, ct));
    [HttpPost("{id:guid}/archive"), RequirePermission(PermissionCatalog.AcademicYear.Archive)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Archive(Guid id, CancellationToken ct) => Result(await service.ArchiveAsync(id, ct));
    [HttpPost("{id:guid}/admissions/open"), RequirePermission(PermissionCatalog.AcademicYear.ManageAdmissions)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> OpenAdmissions(Guid id, OpenAdmissionsRequest request, CancellationToken ct) => Result(await service.OpenAdmissionsAsync(id, request, ct));
    [HttpPost("{id:guid}/admissions/close"), RequirePermission(PermissionCatalog.AcademicYear.ManageAdmissions)] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> CloseAdmissions(Guid id, CancellationToken ct) => Result(await service.CloseAdmissionsAsync(id, ct));
    private ActionResult<ApiResponse<AcademicYearDto>> Result(AcademicYearDto? item) => item is null ? NotFound() : Ok(ApiResponse<AcademicYearDto>.Ok(item));
    private ActionResult<ApiResponse<PagedResult<AcademicYearDto>>> Result(PagedResult<AcademicYearDto>? items) => items is null ? NotFound() : Ok(ApiResponse<PagedResult<AcademicYearDto>>.Ok(items));
}
