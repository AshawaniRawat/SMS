using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
namespace SchoolERP.Api.Extensions;
public static class PipelineExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
        else { app.UseHsts(); app.UseHttpsRedirection(); }
        app.UseCors("Frontend");
        app.MapControllers();
        app.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() })
        });
        return app;
    }
}
