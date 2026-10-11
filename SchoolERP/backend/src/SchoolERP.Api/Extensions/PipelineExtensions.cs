using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
namespace SchoolERP.Api.Extensions;
public static class PipelineExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SchoolERP API v1");
                options.DocumentTitle = "SchoolERP API Documentation";
                options.DisplayRequestDuration();
                options.EnablePersistAuthorization();
            });
        }
        else { app.UseHsts(); app.UseHttpsRedirection(); }
        app.UseCors("Frontend");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() })
        });
        return app;
    }
}
