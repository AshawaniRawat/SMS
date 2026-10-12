using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using SchoolERP.Api.Logging;
using SchoolERP.Api.ErrorHandling;
using SchoolERP.Api.OpenApi;
namespace SchoolERP.Api.Extensions;
public static class PipelineExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages(async status=>{var c=status.HttpContext;if(c.Response.HasStarted||c.Request.Path.StartsWithSegments("/api/health"))return;var(code,message)=c.Response.StatusCode switch{401=>(ErrorCodes.Unauthenticated,"Authentication is required."),403=>(ErrorCodes.PermissionDenied,"You do not have permission to perform this action."),404=>(ErrorCodes.RouteNotFound,"The requested endpoint was not found."),405=>(ErrorCodes.MethodNotAllowed,"The HTTP method is not allowed for this endpoint."),415=>(ErrorCodes.UnsupportedMedia,"The request content type is not supported."),429=>(ErrorCodes.RateLimit,"Too many requests. Please try again later."),_=>("Http.Error","The request could not be completed.")};await ApiErrorWriter.WriteAsync(c,ApiErrorWriter.Create(c,code,message,c.Response.StatusCode),c.RequestAborted);});
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseSchoolErpOpenApi();
        if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
        app.UseCors("Frontend");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<AuthenticatedRequestContextMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() })
        });
        return app;
    }
}
