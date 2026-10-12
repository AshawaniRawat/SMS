using System.Diagnostics;
using Microsoft.Extensions.Options;
using SchoolERP.Api.Configuration;
using SchoolERP.Application.Abstractions;

namespace SchoolERP.Api.Logging;

public sealed class RequestLoggingMiddleware(RequestDelegate next, IOptions<SchoolErpLoggingOptions> options)
{
    public async Task InvokeAsync(HttpContext context, ILogger<RequestLoggingMiddleware> logger, ICurrentUser user, ICurrentTenant tenant)
    {
        var stopwatch = Stopwatch.StartNew();
        var failed = false;
        try
        {
            await next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            // The global handler owns exception logging; only log requests that completed this middleware normally.
            if (!failed)
                LogCompletedRequest(context, logger, user, tenant, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private void LogCompletedRequest(HttpContext context, ILogger<RequestLoggingMiddleware> logger, ICurrentUser user, ICurrentTenant tenant, double elapsedMs)
    {
        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString();
        var arguments = new object?[]
        {
            context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, elapsedMs,
            correlationId, context.TraceIdentifier, user.UserId, tenant.IsAvailable ? tenant.TenantId : null,
            user.SchoolId, user.CampusId
        };
        const string template = "HTTP {Method} {Path} completed {StatusCode} in {ElapsedMs:0.0} ms. " +
                                "CorrelationId {CorrelationId} RequestId {RequestId} UserId {UserId} " +
                                "TenantId {TenantId} SchoolId {SchoolId} CampusId {CampusId}";

        if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(LoggingEventIds.Http.RequestCompleted, template, arguments);
        else if (elapsedMs >= options.Value.SlowRequestThresholdMs && !IsHealthRequest(context))
            logger.LogWarning(LoggingEventIds.Http.SlowRequest, template, arguments);
        else if (!IsHealthRequest(context))
            logger.LogInformation(LoggingEventIds.Http.RequestCompleted, template, arguments);
    }

    private static bool IsHealthRequest(HttpContext context) => context.Request.Path.StartsWithSegments("/api/health");
}
