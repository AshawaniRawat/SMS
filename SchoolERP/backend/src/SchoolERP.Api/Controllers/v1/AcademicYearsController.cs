using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.AcademicYears;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;

namespace SchoolERP.Api.Controllers.v1;

// TODO(P1-auth): Apply AcademicYear.View/Create/Edit/Activate/SetCurrent/Close/Archive/ManageAdmissions policies.
[Route("api/v1/academic-years")]
public sealed class AcademicYearsController(IAcademicYearService service) : ControllerBase
{
    [HttpPost] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Create(CreateAcademicYearRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<AcademicYearDto>.Ok(item)); }
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AcademicYearDto>>>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? schoolId = null, [FromQuery] string? search = null, [FromQuery] AcademicYearStatus? status = null, [FromQuery] bool? isCurrent = null, CancellationToken ct = default) => Ok(ApiResponse<PagedResult<AcademicYearDto>>.Ok(await service.ListAsync(new(pageNumber, pageSize), schoolId, search, status, isCurrent, ct)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Get(Guid id, CancellationToken ct) => Result(await service.GetAsync(id, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/academic-years")] public async Task<ActionResult<ApiResponse<PagedResult<AcademicYearDto>>>> BySchool(Guid schoolId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] AcademicYearStatus? status = null, [FromQuery] bool? isCurrent = null, CancellationToken ct = default) => Ok(ApiResponse<PagedResult<AcademicYearDto>>.Ok(await service.ListAsync(new(pageNumber, pageSize), schoolId, search, status, isCurrent, ct)));
    [HttpGet("/api/v1/schools/{schoolId:guid}/academic-years/current")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Current(Guid schoolId, CancellationToken ct) => Result(await service.GetCurrentAsync(schoolId, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/academic-years/by-code/{code}")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> ByCode(Guid schoolId, string code, CancellationToken ct) => Result(await service.GetByCodeAsync(schoolId, code, ct));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Update(Guid id, UpdateAcademicYearRequest request, CancellationToken ct) => Result(await service.UpdateAsync(id, request, ct));
    [HttpPost("{id:guid}/activate")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Activate(Guid id, CancellationToken ct) => Result(await service.ActivateAsync(id, ct));
    [HttpPost("{id:guid}/set-current")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> SetCurrent(Guid id, CancellationToken ct) => Result(await service.SetCurrentAsync(id, ct));
    [HttpPost("{id:guid}/close")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Close(Guid id, CancellationToken ct) => Result(await service.CloseAsync(id, ct));
    [HttpPost("{id:guid}/archive")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> Archive(Guid id, CancellationToken ct) => Result(await service.ArchiveAsync(id, ct));
    [HttpPost("{id:guid}/admissions/open")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> OpenAdmissions(Guid id, OpenAdmissionsRequest request, CancellationToken ct) => Result(await service.OpenAdmissionsAsync(id, request, ct));
    [HttpPost("{id:guid}/admissions/close")] public async Task<ActionResult<ApiResponse<AcademicYearDto>>> CloseAdmissions(Guid id, CancellationToken ct) => Result(await service.CloseAdmissionsAsync(id, ct));
    private ActionResult<ApiResponse<AcademicYearDto>> Result(AcademicYearDto? item) => item is null ? NotFound() : Ok(ApiResponse<AcademicYearDto>.Ok(item));
}
