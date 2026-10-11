using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Permissions;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;

namespace SchoolERP.Api.Controllers.v1;

// TODO(P1-20): require an explicit permission-catalog view policy.
[Authorize]
public sealed class PermissionsController(IPermissionCatalogService permissions) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PermissionDto>>>> List(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? module = null,
        [FromQuery] PermissionScope? scope = null, [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100) return BadRequest();
        var result = await permissions.GetPermissionsAsync(new PagedRequest(pageNumber, pageSize), module,
            scope, isActive, search, cancellationToken);
        return Ok(ApiResponse<PagedResult<PermissionDto>>.Ok(result));
    }

    [HttpGet("by-code")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> ByCode([FromQuery] string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return BadRequest();
        var result = await permissions.GetPermissionByCodeAsync(code, cancellationToken);
        return result is null ? NotFound() : Ok(ApiResponse<PermissionDto>.Ok(result));
    }

    [HttpGet("modules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PermissionModuleDto>>>> Modules(
        [FromQuery] bool? isActive = null, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<IReadOnlyCollection<PermissionModuleDto>>.Ok(
            await permissions.GetModulesAsync(isActive, cancellationToken)));
}
