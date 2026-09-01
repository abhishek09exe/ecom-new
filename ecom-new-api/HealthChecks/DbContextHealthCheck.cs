using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ecom_new_api.HealthChecks
{
    public class DbContextHealthCheck<TContext>(TContext dbContext) : IHealthCheck where TContext : DbContext
    {
        private readonly TContext _dbContext = dbContext;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bool result = await _dbContext.Database.CanConnectAsync(cancellationToken);
                if (result)
                {
                    return HealthCheckResult.Healthy();
                }
                return HealthCheckResult.Unhealthy("DbContext could not connect to the database");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("DbContext connection test failed", ex);
            }
        }
    }
}
