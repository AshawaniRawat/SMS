using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Security;

namespace SchoolERP.Api.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationService authenticationService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Validates email, password, lockout, account, and organization eligibility. No token is issued in P1-11.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.AuthenticateAsync(request, cancellationToken);
        if (result.IsSuccess) return Ok(ApiResponse<LoginResponse>.Ok(result.Value!));
        var body = ApiResponse<LoginResponse>.Fail([result.Error ?? "Authentication failed."], result.ErrorCode);
        return result.ErrorCode switch
        {
            AuthenticationErrorCodes.InvalidRequest => BadRequest(body),
            AuthenticationErrorCodes.InvalidCredentials => Unauthorized(body),
            AuthenticationErrorCodes.PersistenceFailure => StatusCode(StatusCodes.Status500InternalServerError, body),
            _ => StatusCode(StatusCodes.Status403Forbidden, body)
        };
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<CurrentUserDto>> Me() => Ok(ApiResponse<CurrentUserDto>.Ok(
        new(currentUser.UserId, currentUser.Email, currentUser.TenantId, currentUser.SchoolId, currentUser.CampusId, currentUser.Roles)));
}
