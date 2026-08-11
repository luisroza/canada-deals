using CanadaDeals.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CanadaDeals.Api.Health;

public sealed class DatabaseHealthCheck(DealsDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", ex);
        }
    }
}
