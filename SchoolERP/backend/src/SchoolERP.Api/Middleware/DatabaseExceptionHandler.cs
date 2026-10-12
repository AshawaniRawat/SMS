using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolERP.Api.Logging;

namespace SchoolERP.Api.Middleware;

public sealed class DatabaseExceptionHandler(ILogger<DatabaseExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not (DbUpdateException or NpgsqlException)) return false;
        var correlation=httpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString();
        logger.LogError(LoggingEventIds.Infrastructure.DatabaseFailure,exception, "Database operation failed. CorrelationId {CorrelationId} RequestId {RequestId}",correlation,httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Database temporarily unavailable.",
            Extensions = { ["correlationId"] = correlation, ["requestId"] = httpContext.TraceIdentifier }
        }, cancellationToken);
        return true;
    }
}
