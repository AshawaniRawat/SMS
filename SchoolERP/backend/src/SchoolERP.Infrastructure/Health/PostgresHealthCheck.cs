using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SchoolERP.Infrastructure.Persistence.Context;

namespace SchoolERP.Infrastructure.Health;

public sealed class PostgresHealthCheck(SchoolErpDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
    {
        // CanConnectAsync suppresses provider details. Never return connection strings/exceptions publicly.
        return await context.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database temporarily unavailable.");
    }
}
