using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DT.EmailWorker.Monitoring.HealthChecks
{
    /// <summary>
    /// Health check for SMTP service connectivity
    /// </summary>
    public class SmtpHealthCheck : IHealthCheck
    {
        private readonly ISmtpService _smtpService;

        public SmtpHealthCheck(ISmtpService smtpService)
        {
            _smtpService = smtpService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var isConnected = await _smtpService.TestConnectionAsync();

                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["Connected"] = isConnected
                };

                if (!isConnected)
                {
                    return HealthCheckResult.Unhealthy("SMTP connection failed", data: data);
                }

                if (stopwatch.ElapsedMilliseconds > 10000) // 10 seconds
                {
                    return HealthCheckResult.Degraded($"SMTP responding slowly ({stopwatch.ElapsedMilliseconds}ms)", data: data);
                }

                return HealthCheckResult.Healthy($"SMTP healthy (response: {stopwatch.ElapsedMilliseconds}ms)", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"SMTP health check failed: {ex.Message}", ex);
            }
        }
    }
}