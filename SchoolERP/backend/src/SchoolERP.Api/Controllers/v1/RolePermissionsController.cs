using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Features.RolePermissions;
using SchoolERP.Shared.Models;
using SchoolERP.Shared.Results;
using SchoolERP.Api.Security;
using SchoolERP.Domain.Security;

namespace SchoolERP.Api.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/tenants/{tenantId:guid}/roles/{roleId:guid}/permissions")]
public sealed class RolePermissionsController(IRolePermissionService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.Role.View)]
    public async Task<ActionResult<ApiResponse<RoleWithPermissionsDto>>> Get(Guid tenantId, Guid roleId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        var result = await service.GetRolePermissionsAsync(tenantId, roleId, ct);
        return result is null ? NotFound() : Ok(ApiResponse<RoleWithPermissionsDto>.Ok(result));
    }

    [HttpPost("{permissionId:guid}")]
    [RequirePermission(PermissionCatalog.Role.ManagePermissions)]
    public async Task<IActionResult> Assign(Guid tenantId, Guid roleId, Guid permissionId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        return Mutation(await service.AssignPermissionAsync(tenantId, roleId, permissionId, ct));
    }

    [HttpDelete("{permissionId:guid}")]
    [RequirePermission(PermissionCatalog.Role.ManagePermissions)]
    public async Task<IActionResult> Remove(Guid tenantId, Guid roleId, Guid permissionId, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        return Mutation(await service.RemovePermissionAsync(tenantId, roleId, permissionId, ct));
    }

    [HttpPut]
    [RequirePermission(PermissionCatalog.Role.ManagePermissions)]
    public async Task<ActionResult<ApiResponse<RoleWithPermissionsDto>>> Set(Guid tenantId, Guid roleId,
        SetRolePermissionsRequest request, CancellationToken ct)
    {
        if (!OwnTenant(tenantId)) return NotFound();
        var result = await service.SetPermissionsAsync(tenantId, roleId, request.PermissionIds, ct);
        if (result.IsSuccess) return Ok(ApiResponse<RoleWithPermissionsDto>.Ok(result.Value!));
        return Failure<RoleWithPermissionsDto>(result.Error!, result.ErrorCode);
    }

    private bool OwnTenant(Guid tenantId) => currentUser.IsAuthenticated && currentUser.TenantId == tenantId;
    private IActionResult Mutation(Result<bool> result)
    {
        if (result.IsSuccess) return NoContent();
        var body = ApiResponse<bool>.Fail([result.Error!], result.ErrorCode);
        return result.ErrorCode switch
        {
            RolePermissionErrorCodes.RoleNotFound or RolePermissionErrorCodes.PermissionNotFound => NotFound(body),
            RolePermissionErrorCodes.RoleSystemManaged => StatusCode(StatusCodes.Status403Forbidden, body),
            RolePermissionErrorCodes.RoleArchived => Conflict(body),
            _ => BadRequest(body)
        };
    }
    private ActionResult<ApiResponse<T>> Failure<T>(string error, string? code)
    {
        var body = ApiResponse<T>.Fail([error], code);
        return code switch
        {
            RolePermissionErrorCodes.RoleNotFound or RolePermissionErrorCodes.PermissionNotFound => NotFound(body),
            RolePermissionErrorCodes.RoleSystemManaged => StatusCode(StatusCodes.Status403Forbidden, body),
            RolePermissionErrorCodes.RoleArchived => Conflict(body),
            _ => BadRequest(body)
        };
    }
}
