using DT.EmailWorker.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace DT.EmailWorker.Monitoring.HealthChecks
{
    /// <summary>
    /// Health check for database connectivity and performance
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly EmailDbContext _context;

        public DatabaseHealthCheck(EmailDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Test basic connectivity
                await _context.Database.CanConnectAsync(cancellationToken);

                // Test query performance
                var queueCount = await _context.EmailQueue.CountAsync(cancellationToken);

                stopwatch.Stop();

                var responseTime = stopwatch.ElapsedMilliseconds;
                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = responseTime,
                    ["QueueCount"] = queueCount,
                    ["ConnectionString"] = _context.Database.GetConnectionString()?.Replace("Password=", "Password=***")
                };

                if (responseTime > 5000) // 5 seconds
                {
                    return HealthCheckResult.Degraded($"Database responding slowly ({responseTime}ms)", data: data);
                }

                return HealthCheckResult.Healthy($"Database healthy (response: {responseTime}ms)", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}", ex);
            }
        }
    }
}