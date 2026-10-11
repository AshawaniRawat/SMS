using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchoolERP.Application.Features.Authentication;
using SchoolERP.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Security;
using SchoolERP.Application.Common.Pagination;
using FluentValidation;

namespace SchoolERP.Api.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationService authenticationService, IRefreshTokenService refreshTokenService,
    ISessionService sessionService, IPasswordRecoveryService passwordRecoveryService,
    IValidator<ForgotPasswordRequest> forgotPasswordValidator, IClientRequestContext clientRequest,
    ICurrentUser currentUser) : ControllerBase
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

    /// <summary>Rotates a valid refresh token and returns a new access/refresh token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(typeof(ApiResponse<TokenPairResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TokenPairResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TokenPairResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<TokenPairResponse>>> Refresh(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await refreshTokenService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (result.IsSuccess) return Ok(ApiResponse<TokenPairResponse>.Ok(result.Value!));
        var body = ApiResponse<TokenPairResponse>.Fail([result.Error ?? "Token refresh failed."], result.ErrorCode);
        return result.ErrorCode switch
        {
            RefreshTokenErrorCodes.InvalidRequest => BadRequest(body),
            RefreshTokenErrorCodes.PersistenceFailure => StatusCode(StatusCodes.Status500InternalServerError, body),
            _ => Unauthorized(body)
        };
    }

    /// <summary>Accepts a recovery request without revealing whether the email belongs to an account.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<bool>.Fail(validation.Errors.Select(x => x.ErrorMessage)));
        await passwordRecoveryService.RequestPasswordResetAsync(request.Email, clientRequest.IpAddress, cancellationToken);
        return Accepted(ApiResponse<bool>.Ok(true, PasswordRecoveryMessages.ForgotPasswordAccepted));
    }

    /// <summary>Consumes an Identity password-reset token. Successful reset requires a normal login afterward.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await passwordRecoveryService.ResetPasswordAsync(request, clientRequest.IpAddress, cancellationToken);
        if (result.IsSuccess) return Ok(ApiResponse<bool>.Ok(true, PasswordRecoveryMessages.ResetSucceeded));
        var body = ApiResponse<bool>.Fail([result.Error ?? PasswordRecoveryMessages.InvalidReset], result.ErrorCode);
        return result.ErrorCode == PasswordRecoveryErrorCodes.SessionRevocationFailed
            ? StatusCode(StatusCodes.Status500InternalServerError, body)
            : BadRequest(body);
    }

    /// <summary>Revokes the complete refresh-token chain identified by the supplied token. Safe to repeat.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sessionService.LogoutSessionAsync(request.RefreshToken, cancellationToken);
        if (result.IsSuccess) return NoContent();
        return BadRequest(ApiResponse<bool>.Fail([result.Error ?? "The logout request is invalid."], result.ErrorCode));
    }

    /// <summary>Revokes every refresh-token session owned by the authenticated user.</summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await sessionService.LogoutAllAsync(currentUser.UserId!.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns one safe, aggregated row per refresh-token session.</summary>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSessionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserSessionDto>>>> Sessions(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100)
            return BadRequest(ApiResponse<PagedResult<UserSessionDto>>.Fail(["Page number must be positive and page size must be between 1 and 100."]));
        var result = await sessionService.GetSessionsAsync(currentUser.UserId!.Value,
            new PagedRequest(pageNumber, pageSize), activeOnly, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserSessionDto>>.Ok(result));
    }

    /// <summary>Revokes an owned refresh-token session. Already-revoked sessions remain idempotent.</summary>
    [HttpPost("sessions/{sessionId}/revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(string sessionId, CancellationToken cancellationToken)
    {
        var result = await sessionService.RevokeSessionByIdAsync(currentUser.UserId!.Value, sessionId, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<CurrentUserDto>> Me() => Ok(ApiResponse<CurrentUserDto>.Ok(
        new(currentUser.UserId, currentUser.Email, currentUser.TenantId, currentUser.SchoolId, currentUser.CampusId, currentUser.Roles)));
}
