using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DT.EmailWorker.Monitoring.HealthChecks
{
    /// <summary>
    /// Health check for email queue status and performance
    /// </summary>
    public class QueueHealthCheck : IHealthCheck
    {
        private readonly IEmailQueueService _queueService;

        public QueueHealthCheck(IEmailQueueService queueService)
        {
            _queueService = queueService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var stats = await _queueService.GetQueueStatisticsAsync(cancellationToken);

                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["PendingEmails"] = stats.PendingCount,
                    ["ProcessingEmails"] = stats.ProcessingCount,
                    ["FailedEmails"] = stats.FailedCount,
                    ["TotalEmails"] = stats.TotalCount
                };

                // Check for concerning queue conditions
                if (stats.PendingCount > 10000)
                {
                    return HealthCheckResult.Degraded($"Large queue backlog: {stats.PendingCount} pending emails", data: data);
                }

                if (stats.FailedCount > stats.SentCount * 0.1) // More than 10% failure rate
                {
                    return HealthCheckResult.Degraded($"High failure rate: {stats.FailedCount} failed vs {stats.SentCount} sent", data: data);
                }

                if (stopwatch.ElapsedMilliseconds > 3000) // 3 seconds
                {
                    return HealthCheckResult.Degraded($"Queue responding slowly ({stopwatch.ElapsedMilliseconds}ms)", data: data);
                }

                return HealthCheckResult.Healthy($"Queue healthy - {stats.PendingCount} pending, {stats.ProcessingCount} processing", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Queue health check failed: {ex.Message}", ex);
            }
        }
    }
}