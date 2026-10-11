using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Api.Security;
using SchoolERP.Application.Features.PlatformDashboard;
using SchoolERP.Domain.Security;
using SchoolERP.Shared.Models;

namespace SchoolERP.Api.Controllers.v1;

[ApiController]
[Authorize]
[Route("api/v1/platform/dashboard")]
public sealed class PlatformDashboardController(IPlatformDashboardService service) : ControllerBase
{
    /// <summary>Returns aggregate, privacy-limited operational metrics for authorized platform administrators.</summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.Platform.ViewDashboard)]
    public async Task<ActionResult<ApiResponse<PlatformDashboardDto>>> Get(
        [FromQuery] int trendMonths = 12, [FromQuery] int recentTenantLimit = 10, CancellationToken ct = default) =>
        Ok(ApiResponse<PlatformDashboardDto>.Ok(await service.GetDashboardAsync(
            new(trendMonths, recentTenantLimit), ct)));
}
