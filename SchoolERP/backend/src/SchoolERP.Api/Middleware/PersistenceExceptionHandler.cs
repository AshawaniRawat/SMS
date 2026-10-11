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
            ResourceNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access is denied."),
            SchoolSelectionRequiredException => (StatusCodes.Status409Conflict, exception.Message),
            SchoolArchivedReadOnlyException => (StatusCodes.Status409Conflict, exception.Message),
            CampusArchivedReadOnlyException => (StatusCodes.Status409Conflict, exception.Message),
            MainCampusCannotBeSuspendedException => (StatusCodes.Status409Conflict, exception.Message),
            MainCampusCannotBeArchivedException => (StatusCodes.Status409Conflict, exception.Message),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "The record was changed by another operation."),
            TenantSlugConflictException => (StatusCodes.Status409Conflict, "A tenant with this slug already exists."),
            SchoolCodeConflictException => (StatusCodes.Status409Conflict, "A school with this code already exists in the tenant."),
            CampusCodeConflictException => (StatusCodes.Status409Conflict, exception.Message),
            MainCampusConflictException => (StatusCodes.Status409Conflict, exception.Message),
            ParentNotOperationalException => (StatusCodes.Status409Conflict, exception.Message),
            AcademicYearCodeConflictException => (StatusCodes.Status409Conflict, exception.Message),
            AcademicYearOverlapException => (StatusCodes.Status409Conflict, exception.Message),
            CurrentAcademicYearConflictException => (StatusCodes.Status409Conflict, exception.Message),
            TenantNotOperationalException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidTenantStatusTransitionException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidSchoolStatusTransitionException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidCampusStatusTransitionException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidAcademicYearStatusTransitionException => (StatusCodes.Status409Conflict, exception.Message),
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
