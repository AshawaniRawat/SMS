using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Campuses;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;

namespace SchoolERP.Api.Controllers.v1;

// TODO(P1-auth): Apply Campus.View/Create/Edit/Activate/Suspend/Archive/SetMain policies.
public sealed class CampusesController(ICampusService service) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CampusDto>>> Create(CreateCampusRequest request, CancellationToken ct) { var item = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<CampusDto>.Ok(item)); }
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CampusDto>>>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? schoolId = null, [FromQuery] string? search = null, [FromQuery] CampusStatus? status = null,
        [FromQuery] bool? isMainCampus = null, CancellationToken ct = default) => Ok(ApiResponse<PagedResult<CampusDto>>.Ok(await service.ListAsync(new(pageNumber, pageSize), schoolId, search, status, isMainCampus, ct)));
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CampusDto>>> Get(Guid id, CancellationToken ct) => Result(await service.GetByIdAsync(id, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/campuses")]
    public async Task<ActionResult<ApiResponse<PagedResult<CampusDto>>>> BySchool(Guid schoolId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] CampusStatus? status = null, [FromQuery] bool? isMainCampus = null, CancellationToken ct = default) =>
        Ok(ApiResponse<PagedResult<CampusDto>>.Ok(await service.ListAsync(new(pageNumber, pageSize), schoolId, search, status, isMainCampus, ct)));
    [HttpGet("/api/v1/schools/{schoolId:guid}/campuses/by-code/{code}")]
    public async Task<ActionResult<ApiResponse<CampusDto>>> ByCode(Guid schoolId, string code, CancellationToken ct) => Result(await service.GetByCodeAsync(schoolId, code, ct));
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CampusDto>>> Update(Guid id, CampusProfileRequest request, CancellationToken ct) => Result(await service.UpdateAsync(id, request, ct));
    [HttpPut("{id:guid}/code")]
    public async Task<ActionResult<ApiResponse<CampusDto>>> ChangeCode(Guid id, ChangeCampusCodeRequest request, CancellationToken ct) => Result(await service.ChangeCodeAsync(id, request, ct));
    [HttpPost("{id:guid}/set-main")] public async Task<ActionResult<ApiResponse<CampusDto>>> SetMain(Guid id, CancellationToken ct) => Result(await service.SetMainAsync(id, ct));
    [HttpPost("{id:guid}/activate")] public async Task<ActionResult<ApiResponse<CampusDto>>> Activate(Guid id, CancellationToken ct) => Result(await service.ActivateAsync(id, ct));
    [HttpPost("{id:guid}/suspend")] public async Task<ActionResult<ApiResponse<CampusDto>>> Suspend(Guid id, CancellationToken ct) => Result(await service.SuspendAsync(id, ct));
    [HttpPost("{id:guid}/reactivate")] public async Task<ActionResult<ApiResponse<CampusDto>>> Reactivate(Guid id, CancellationToken ct) => Result(await service.ReactivateAsync(id, ct));
    [HttpPost("{id:guid}/archive")] public async Task<ActionResult<ApiResponse<CampusDto>>> Archive(Guid id, CancellationToken ct) => Result(await service.ArchiveAsync(id, ct));
    private ActionResult<ApiResponse<CampusDto>> Result(CampusDto? item) => item is null ? NotFound() : Ok(ApiResponse<CampusDto>.Ok(item));
}
