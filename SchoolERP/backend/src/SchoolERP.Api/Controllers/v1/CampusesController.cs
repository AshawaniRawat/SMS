using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Campuses;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;
using SchoolERP.Api.Security;
using SchoolERP.Domain.Security;

namespace SchoolERP.Api.Controllers.v1;

public sealed class CampusesController(ICampusService service) : ApiControllerBase
{
    [HttpPost]
    [RequirePermission(PermissionCatalog.Campus.Create)]
    public async Task<ActionResult<ApiResponse<CampusDetailsDto>>> Create(CreateCampusRequest request, CancellationToken ct) { var item = await service.CreateCampusAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<CampusDetailsDto>.Ok(item)); }
    [HttpGet]
    [RequirePermission(PermissionCatalog.Campus.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<CampusListItemDto>>>> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] CampusStatus? status = null, [FromQuery] bool? isMain = null,
        [FromQuery] string? city = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null,
        CancellationToken ct = default) => Ok(ApiResponse<PagedResult<CampusListItemDto>>.Ok(await service.GetCampusesAsync(new(pageNumber, pageSize, search, status, isMain, city, sortBy, sortDirection), ct)));
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCatalog.Campus.View)]
    public async Task<ActionResult<ApiResponse<CampusDetailsDto>>> Get(Guid id, CancellationToken ct) { var item = await service.GetCampusAsync(id, ct); return item is null ? NotFound() : Ok(ApiResponse<CampusDetailsDto>.Ok(item)); }
    [HttpGet("/api/v1/schools/{schoolId:guid}/campuses")]
    [RequirePermission(PermissionCatalog.Campus.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<CampusDto>>>> BySchool(Guid schoolId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] CampusStatus? status = null, [FromQuery] bool? isMainCampus = null, CancellationToken ct = default) =>
        Result(await service.ListForSchoolAsync(new(pageNumber, pageSize), schoolId, search, status, isMainCampus, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/campuses/{campusId:guid}")]
    [RequirePermission(PermissionCatalog.Campus.View)]
    public async Task<ActionResult<ApiResponse<CampusDto>>> ForSchool(Guid schoolId, Guid campusId, CancellationToken ct) =>
        Result(await service.GetForSchoolAsync(schoolId, campusId, ct));
    [HttpGet("/api/v1/schools/{schoolId:guid}/campuses/by-code/{code}")]
    [RequirePermission(PermissionCatalog.Campus.View)]
    public async Task<ActionResult<ApiResponse<CampusDto>>> ByCode(Guid schoolId, string code, CancellationToken ct) => Result(await service.GetByCodeAsync(schoolId, code, ct));
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCatalog.Campus.Edit)]
    public async Task<ActionResult<ApiResponse<CampusDetailsDto>>> Update(Guid id, CampusProfileRequest request, CancellationToken ct) { var item = await service.UpdateCampusAsync(id, request, ct); return item is null ? NotFound() : Ok(ApiResponse<CampusDetailsDto>.Ok(item)); }
    [HttpPut("{id:guid}/code")]
    [RequirePermission(PermissionCatalog.Campus.Edit)]
    public async Task<ActionResult<ApiResponse<CampusDto>>> ChangeCode(Guid id, ChangeCampusCodeRequest request, CancellationToken ct) => Result(await service.ChangeCodeAsync(id, request, ct));
    [HttpPost("{id:guid}/set-main"), RequirePermission(PermissionCatalog.Campus.SetMain)] public async Task<ActionResult<ApiResponse<CampusDto>>> SetMain(Guid id, CancellationToken ct) => Result(await service.SetMainAsync(id, ct));
    [HttpPost("{id:guid}/activate"), RequirePermission(PermissionCatalog.Campus.Activate)] public async Task<ActionResult<ApiResponse<CampusDto>>> Activate(Guid id, CancellationToken ct) => Result(await service.ActivateAsync(id, ct));
    [HttpPost("{id:guid}/suspend"), RequirePermission(PermissionCatalog.Campus.Suspend)] public async Task<ActionResult<ApiResponse<CampusDto>>> Suspend(Guid id, CancellationToken ct) => Result(await service.SuspendAsync(id, ct));
    [HttpPost("{id:guid}/reactivate"), RequirePermission(PermissionCatalog.Campus.Activate)] public async Task<ActionResult<ApiResponse<CampusDto>>> Reactivate(Guid id, CancellationToken ct) => Result(await service.ReactivateAsync(id, ct));
    [HttpPost("{id:guid}/archive"), RequirePermission(PermissionCatalog.Campus.Archive)] public async Task<ActionResult<ApiResponse<CampusDto>>> Archive(Guid id, CancellationToken ct) => Result(await service.ArchiveAsync(id, ct));
    private ActionResult<ApiResponse<CampusDto>> Result(CampusDto? item) => item is null ? NotFound() : Ok(ApiResponse<CampusDto>.Ok(item));
    private ActionResult<ApiResponse<PagedResult<CampusDto>>> Result(PagedResult<CampusDto>? items) =>
        items is null ? NotFound() : Ok(ApiResponse<PagedResult<CampusDto>>.Ok(items));
}
