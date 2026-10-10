using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Features.Identity;
using SchoolERP.Shared.Models;
using SchoolERP.Shared.Results;

namespace SchoolERP.Api.Controllers.v1;

// Development-only until P1-17 authorization policies can protect administrative account creation.
[ApiController]
public sealed class UserRegistrationController(IUserIdentityService identityService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("api/v1/platform/users")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> CreatePlatformUser(
        CreatePlatformUserRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return ToResponse(await identityService.CreatePlatformUserAsync(request, cancellationToken));
    }

    [HttpPost("api/v1/tenants/{tenantId:guid}/users")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> CreateTenantUser(Guid tenantId,
        CreateTenantUserApiRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var command = new CreateTenantUserRequest(tenantId, request.FirstName, request.LastName, request.Email,
            request.Password, request.SchoolId, request.CampusId, request.DisplayName, request.PhoneNumber);
        return ToResponse(await identityService.CreateTenantUserAsync(command, cancellationToken));
    }

    private ActionResult<ApiResponse<UserSummaryDto>> ToResponse(Result<UserSummaryDto> result)
    {
        if (result.IsSuccess) return StatusCode(StatusCodes.Status201Created, ApiResponse<UserSummaryDto>.Ok(result.Value!));
        var body = ApiResponse<UserSummaryDto>.Fail([result.Error ?? "User creation failed."], result.ErrorCode);
        return result.ErrorCode switch
        {
            UserRegistrationErrorCodes.EmailAlreadyExists => Conflict(body),
            UserRegistrationErrorCodes.ScopeNotFound => NotFound(body),
            UserRegistrationErrorCodes.ScopeNotOperational => Conflict(body),
            _ => BadRequest(body)
        };
    }
}
