using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Api.Security;
using SchoolERP.Application.Features.SchoolDashboard;
using SchoolERP.Domain.Security;
using SchoolERP.Shared.Models;

namespace SchoolERP.Api.Controllers.v1;

[ApiController, Authorize, Route("api/v1/dashboard")]
public sealed class SchoolDashboardController(ISchoolDashboardService service) : ControllerBase
{
    /// <summary>Returns the authenticated user's tenant- and school-scoped operational dashboard.</summary>
    [HttpGet, RequirePermission(PermissionCatalog.School.ViewDashboard)]
    public async Task<ActionResult<ApiResponse<SchoolDashboardDto>>> Get(CancellationToken ct) =>
        Ok(ApiResponse<SchoolDashboardDto>.Ok(await service.GetDashboardAsync(ct)));
}
