using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Security;
using SchoolERP.Shared.Models;
using SchoolERP.Api.Logging;

namespace SchoolERP.Api.Security;

public static class PermissionPolicy
{
    public const string Prefix = "Permission:";
    public static string Name(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        return Prefix + permissionCode;
    }
}

public sealed record PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        PermissionCode = permissionCode;
    }
    public string PermissionCode { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode) => Policy = PermissionPolicy.Name(permissionCode);
}

public sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PermissionPolicy.Prefix, StringComparison.Ordinal))
            return base.GetPolicyAsync(policyName);
        var code = policyName[PermissionPolicy.Prefix.Length..];
        if (string.IsNullOrWhiteSpace(code)) return Task.FromResult<AuthorizationPolicy?>(null);
        return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(code)).Build());
    }
}

public sealed class PermissionAuthorizationHandler(IUserPermissionService permissions, ICurrentUser currentUser,
    IHttpContextAccessor accessor, ILogger<PermissionAuthorizationHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId) return;
        var http = accessor.HttpContext;
        if (await permissions.HasPermissionAsync(userId, requirement.PermissionCode,
            http?.RequestAborted ?? CancellationToken.None))
        {
            context.Succeed(requirement); return;
        }
        logger.LogWarning(LoggingEventIds.Security.PermissionDenied,"Permission denied. UserId {UserId}, TenantId {TenantId}, Permission {PermissionCode}, Method {Method}, Path {Path}",
            userId, currentUser.TenantId, requirement.PermissionCode, http?.Request.Method, http?.Request.Path.Value);
    }
}

public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(["Authentication is required."],
                "Authorization.Unauthenticated"), context.RequestAborted); return;
        }
        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                ["You do not have permission to perform this action."], "Authorization.PermissionDenied"),
                context.RequestAborted); return;
        }
        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
