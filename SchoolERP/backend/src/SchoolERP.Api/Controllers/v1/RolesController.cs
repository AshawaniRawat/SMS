using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Roles;
using SchoolERP.Domain.Enums;
using SchoolERP.Shared.Models;
using SchoolERP.Shared.Results;

namespace SchoolERP.Api.Controllers.v1;

// TODO(P1-20): replace the authenticated same-tenant gate with explicit role-management permissions.
[ApiController]
[Authorize]
[Route("api/v1/tenants/{tenantId:guid}/roles")]
public sealed class RolesController(IRoleManagementService roles, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create(Guid tenantId, CreateTenantRoleRequest request,
        CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        var result = await roles.CreateTenantRoleAsync(tenantId, request, ct);
        if (result.IsSuccess) return CreatedAtAction(nameof(Get), new { tenantId, roleId = result.Value!.Id },
            ApiResponse<RoleDto>.Ok(result.Value));
        return RoleFailure(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleDto>>>> List(Guid tenantId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] RoleStatus? status = null,
        [FromQuery] RoleType? type = null, [FromQuery] bool? isSystemRole = null, [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        if (pageNumber < 1 || pageSize is < 1 or > 100) return BadRequest();
        var result = await roles.ListTenantRolesAsync(tenantId, new(pageNumber, pageSize), status, type,
            isSystemRole, search, ct);
        return Ok(ApiResponse<PagedResult<RoleDto>>.Ok(result));
    }

    [HttpGet("{roleId:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Get(Guid tenantId, Guid roleId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        var role = await roles.GetTenantVisibleRoleAsync(tenantId, roleId, ct);
        return role is null ? NotFound() : Ok(ApiResponse<RoleDto>.Ok(role));
    }

    [HttpPut("{roleId:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(Guid tenantId, Guid roleId, UpdateRoleRequest request,
        CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        return RoleResult(await roles.UpdateTenantRoleAsync(tenantId, roleId, request, ct));
    }

    [HttpPost("{roleId:guid}/activate")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Activate(Guid tenantId, Guid roleId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        return RoleResult(await roles.ActivateAsync(tenantId, roleId, ct));
    }

    [HttpPost("{roleId:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Deactivate(Guid tenantId, Guid roleId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        return RoleResult(await roles.DeactivateAsync(tenantId, roleId, ct));
    }

    [HttpPost("{roleId:guid}/archive")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Archive(Guid tenantId, Guid roleId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        return RoleResult(await roles.ArchiveAsync(tenantId, roleId, ct));
    }

    [HttpPost("{roleId:guid}/users/{userId:guid}")]
    public async Task<IActionResult> Assign(Guid tenantId, Guid roleId, Guid userId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        var result = await roles.AssignRoleAsync(tenantId, userId, roleId, ct);
        return AssignmentResult(result);
    }

    [HttpDelete("{roleId:guid}/users/{userId:guid}")]
    public async Task<IActionResult> Remove(Guid tenantId, Guid roleId, Guid userId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        var result = await roles.RemoveRoleAsync(tenantId, userId, roleId, ct);
        return AssignmentResult(result);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleDto>>>> UserRoles(Guid tenantId, Guid userId,
        CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        var result = await roles.GetUserRolesAsync(tenantId, userId, ct);
        return Ok(ApiResponse<IReadOnlyCollection<RoleDto>>.Ok(result));
    }

    private bool OwnTenant(Guid tenantId) => currentUser.IsAuthenticated && currentUser.TenantId == tenantId;
    private ActionResult<ApiResponse<RoleDto>> RoleResult(Result<RoleDto> result) =>
        result.IsSuccess ? Ok(ApiResponse<RoleDto>.Ok(result.Value!)) : RoleFailure(result);
    private ActionResult<ApiResponse<RoleDto>> RoleFailure(Result<RoleDto> result)
    {
        var body = ApiResponse<RoleDto>.Fail([result.Error ?? "Role operation failed."], result.ErrorCode);
        return result.ErrorCode switch
        {
            RoleErrorCodes.NotFound => NotFound(body),
            RoleErrorCodes.DuplicateCode or RoleErrorCodes.HasAssignedUsers => Conflict(body),
            RoleErrorCodes.SystemRoleProtected => StatusCode(StatusCodes.Status403Forbidden, body),
            _ => BadRequest(body)
        };
    }
    private IActionResult AssignmentResult(Result<bool> result)
    {
        if (result.IsSuccess) return NoContent();
        var body = ApiResponse<bool>.Fail([result.Error ?? "Role assignment failed."], result.ErrorCode);
        return result.ErrorCode is RoleErrorCodes.NotFound or RoleErrorCodes.UserNotFound ? NotFound(body) : BadRequest(body);
    }
}
