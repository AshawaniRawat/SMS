using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Schools;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;

namespace SchoolERP.Api.Controllers.v1;

// TODO(P1-auth): Require the future tenant SchoolAdmin authorization policy.
public sealed class SchoolsController(ISchoolService schoolService) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SchoolDto>>> Create(CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        var school = await schoolService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = school.Id }, ApiResponse<SchoolDto>.Ok(school));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SchoolDto>>>> List(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] SchoolStatus? status = null,
        [FromQuery] SchoolBoardType? boardType = null, CancellationToken cancellationToken = default)
    {
        var result = await schoolService.ListAsync(new PagedRequest(pageNumber, pageSize), search, status, boardType, cancellationToken);
        return Ok(ApiResponse<PagedResult<SchoolDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SchoolDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var school = await schoolService.GetByIdAsync(id, cancellationToken);
        return school is null ? NotFound() : Ok(ApiResponse<SchoolDto>.Ok(school));
    }

    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<ApiResponse<SchoolDto>>> GetByCode(string code, CancellationToken cancellationToken)
    {
        var school = await schoolService.GetByCodeAsync(code, cancellationToken);
        return school is null ? NotFound() : Ok(ApiResponse<SchoolDto>.Ok(school));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SchoolDto>>> UpdateProfile(Guid id, SchoolProfileRequest request,
        CancellationToken cancellationToken)
    {
        var school = await schoolService.UpdateProfileAsync(id, request, cancellationToken);
        return school is null ? NotFound() : Ok(ApiResponse<SchoolDto>.Ok(school));
    }

    [HttpPost("{id:guid}/activate")]
    public Task<ActionResult<ApiResponse<SchoolDto>>> Activate(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => schoolService.ActivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/suspend")]
    public Task<ActionResult<ApiResponse<SchoolDto>>> Suspend(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => schoolService.SuspendAsync(id, cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    public Task<ActionResult<ApiResponse<SchoolDto>>> Reactivate(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => schoolService.ReactivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/archive")]
    public Task<ActionResult<ApiResponse<SchoolDto>>> Archive(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(() => schoolService.ArchiveAsync(id, cancellationToken));

    private async Task<ActionResult<ApiResponse<SchoolDto>>> ChangeStatus(Func<Task<SchoolDto?>> operation)
    {
        var school = await operation();
        return school is null ? NotFound() : Ok(ApiResponse<SchoolDto>.Ok(school));
    }
}
