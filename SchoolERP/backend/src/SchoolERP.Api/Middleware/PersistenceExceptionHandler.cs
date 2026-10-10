using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Exceptions;
using FluentValidation;

namespace SchoolERP.Api.Middleware;

public sealed class PersistenceExceptionHandler(ILogger<PersistenceExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            TenantContextMissingException => (StatusCodes.Status400BadRequest, "Tenant context is required."),
            TenantMismatchException => (StatusCodes.Status403Forbidden, "The operation is not allowed for this tenant."),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "The record was changed by another operation."),
            TenantSlugConflictException => (StatusCodes.Status409Conflict, "A tenant with this slug already exists."),
            SchoolCodeConflictException => (StatusCodes.Status409Conflict, "A school with this code already exists in the tenant."),
            TenantNotOperationalException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidTenantStatusTransitionException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidSchoolStatusTransitionException => (StatusCodes.Status409Conflict, exception.Message),
            ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            ArgumentOutOfRangeException => (StatusCodes.Status400BadRequest, "One or more request values are outside the allowed range."),
            _ => (0, string.Empty)
        };
        if (status == 0) return false;

        logger.LogWarning("Persistence operation rejected with status {StatusCode}. TraceId: {TraceId}", status, httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, cancellationToken);
        return true;
    }
}
