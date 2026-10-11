using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Api.Security;
using SchoolERP.Application.Features.SchoolProfile;
using SchoolERP.Domain.Security;
using SchoolERP.Shared.Models;

namespace SchoolERP.Api.Controllers.v1;

[ApiController, Authorize, Route("api/v1/school/profile")]
public sealed class SchoolProfileController(ISchoolProfileService service) : ControllerBase
{
    /// <summary>Returns the profile for the school resolved from the authenticated context.</summary>
    [HttpGet, RequirePermission(PermissionCatalog.School.View)]
    public async Task<ActionResult<ApiResponse<SchoolProfileDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse<SchoolProfileDto>.Ok(await service.GetCurrentAsync(cancellationToken)));

    /// <summary>Updates editable profile fields for the school resolved from the authenticated context.</summary>
    [HttpPut, RequirePermission(PermissionCatalog.School.Edit)]
    public async Task<ActionResult<ApiResponse<SchoolProfileDto>>> Update(
        UpdateSchoolProfileRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SchoolProfileDto>.Ok(await service.UpdateCurrentAsync(request, cancellationToken)));
}
