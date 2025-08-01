using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DT.EmailWorker.Workers
{
    /// <summary>
    /// Background worker for monitoring service health and performance
    /// </summary>
    public class HealthCheckWorker : BackgroundService
    {
        private readonly ILogger<HealthCheckWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EmailWorkerSettings _settings;
        private readonly ProcessingSettings _processingSettings;

        public HealthCheckWorker(
            ILogger<HealthCheckWorker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<EmailWorkerSettings> settings,
            IOptions<ProcessingSettings> processingSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
            _processingSettings = processingSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Health Check Worker starting");

            // Wait for the main worker to start
            await Task.Delay(10000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformHealthChecksAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Health Check Worker shutting down");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health check worker");

                    // Try to log the error to the health service
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();
                        await healthService.LogProcessingErrorAsync(null, "Health check worker error", ex.ToString());
                    }
                    catch
                    {
                        // Ignore errors in error logging to prevent cascading failures
                    }

                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }

                // Use HealthCheckIntervalMinutes from EmailWorkerSettings
                await Task.Delay(TimeSpan.FromMinutes(_settings.HealthCheckIntervalMinutes), stoppingToken);
            }

            _logger.LogInformation("Health Check Worker stopped");
        }

        private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();
            var queueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            try
            {
                _logger.LogDebug("Starting health check cycle");

                // Perform comprehensive health check
                var healthResult = await healthService.PerformHealthCheckAsync();

                // Update service status based on health check results
                await healthService.UpdateServiceStatusAsync(healthResult.OverallStatus);

                // Check for stuck emails and reset them
                var stuckCount = await queueService.ResetStuckEmailsAsync(_processingSettings.MaxProcessingTimeMinutes);
                if (stuckCount > 0)
                {
                    _logger.LogWarning("Reset {StuckCount} stuck emails", stuckCount);
                    await healthService.LogProcessingInfoAsync(null,
                        $"Reset {stuckCount} stuck emails", "HealthCheckWorker", "StuckEmailReset");
                }

                // Get queue statistics for monitoring
                var queueStats = await queueService.GetQueueStatisticsAsync();

                // Check for concerning queue conditions
                await CheckQueueHealth(queueStats, healthService);

                // Get performance metrics
                var performanceMetrics = await healthService.GetPerformanceMetricsAsync(1); // Last hour

                // Update detailed performance metrics
                await healthService.UpdatePerformanceMetricsAsync(new PerformanceMetrics
                {
                    EmailsProcessedLastHour = performanceMetrics.TotalEmailsProcessed,
                    EmailsFailedLastHour = performanceMetrics.TotalEmailsFailed,
                    AverageProcessingTimeMs = performanceMetrics.AverageProcessingTimeMs,
                    // FIXED: Use correct property names from QueueStatistics
                    CurrentQueueDepth = queueStats.TotalQueued,
                    ActiveWorkers = _processingSettings.MaxConcurrentWorkers
                });

                // Log health summary
                if (healthResult.OverallStatus == ServiceHealthStatus.Healthy)
                {
                    _logger.LogDebug("Health check completed - Service is healthy. Queue depth: {QueueDepth}, " +
                        "Processing: {ProcessingCount}",
                        queueStats.TotalQueued, queueStats.ProcessingCount);
                }
                else
                {
                    _logger.LogWarning("Health check completed - Service status: {Status}. Issues: {Issues}",
                        healthResult.OverallStatus, healthResult.Summary);

                    // Send health alert for critical issues
                    if (healthResult.OverallStatus == ServiceHealthStatus.Critical)
                    {
                        await healthService.SendHealthAlertAsync(AlertLevel.Critical,
                            healthResult.Summary,
                            new Dictionary<string, object>
                            {
                                ["QueueDepth"] = queueStats.TotalQueued,
                                ["FailedComponents"] = healthResult.ComponentResults
                                    .Where(r => r.Status != HealthStatus.Healthy)
                                    .Select(r => r.Description ?? "Unknown component")
                                    .ToList()
                            });
                    }
                }

                // Check for unhealthy service instances
                var unhealthyServices = await healthService.GetUnhealthyServicesAsync(15); // 15 minute threshold
                if (unhealthyServices.Any())
                {
                    _logger.LogWarning("Found {Count} unhealthy service instances", unhealthyServices.Count);

                    foreach (var service in unhealthyServices)
                    {
                        _logger.LogWarning("Unhealthy service: {ServiceName} on {MachineName}, " +
                            "Status: {Status}, Last heartbeat: {LastHeartbeat}",
                            service.ServiceName, service.MachineName, service.Status, service.LastHeartbeat);
                    }
                }

                _logger.LogDebug("Health check cycle completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health checks");

                // Try to update status to indicate health check failure
                try
                {
                    await healthService.UpdateServiceStatusAsync(ServiceHealthStatus.Warning);
                    await healthService.LogProcessingErrorAsync(null, "Health check failed", ex.ToString(), "HealthCheckWorker");
                }
                catch
                {
                    // Ignore nested errors
                }

                throw;
            }
        }

        private async Task CheckQueueHealth(QueueStatistics queueStats, IHealthService healthService)
        {
            try
            {
                // Check for high queue depth
                if (queueStats.TotalQueued > 1000)
                {
                    var alertLevel = queueStats.TotalQueued > 5000 ? AlertLevel.Critical : AlertLevel.Warning;

                    await healthService.SendHealthAlertAsync(alertLevel,
                        $"High queue depth detected: {queueStats.TotalQueued} emails queued",
                        new Dictionary<string, object>
                        {
                            ["QueueDepth"] = queueStats.TotalQueued,
                            ["ProcessingCount"] = queueStats.ProcessingCount,
                            ["FailedCount"] = queueStats.FailedCount
                        });
                }

                // Check for high failure rate
                // FIXED: Use correct property names from QueueStatistics
                var totalProcessed = queueStats.SentCount + queueStats.FailedCount;
                if (totalProcessed > 0)
                {
                    var failureRate = (double)queueStats.FailedCount / totalProcessed * 100;
                    if (failureRate > 10) // More than 10% failure rate
                    {
                        await healthService.SendHealthAlertAsync(AlertLevel.Warning,
                            $"High failure rate detected: {failureRate:F1}% of emails are failing",
                            new Dictionary<string, object>
                            {
                                ["FailureRate"] = failureRate,
                                ["TotalFailed"] = queueStats.FailedCount,
                                ["TotalProcessed"] = totalProcessed
                            });
                    }
                }

                // Check for processing slowdown - calculate processing rate from available data
                // FIXED: Calculate processing rate since it's not available as a property
                var timeSinceLastUpdate = DateTime.UtcNow.AddHours(3) - queueStats.LastUpdated;
                var processingRate = timeSinceLastUpdate.TotalHours > 0
                    ? queueStats.ProcessingCount / timeSinceLastUpdate.TotalHours
                    : 0;

                if (processingRate < 10 && queueStats.TotalQueued > 100) // Less than 10 emails/hour with backlog
                {
                    await healthService.SendHealthAlertAsync(AlertLevel.Warning,
                        $"Processing slowdown detected: {processingRate:F1} emails/hour with {queueStats.TotalQueued} queued",
                        new Dictionary<string, object>
                        {
                            ["ProcessingRate"] = processingRate,
                            ["QueueDepth"] = queueStats.TotalQueued,
                            ["ProcessingCount"] = queueStats.ProcessingCount
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking queue health");
            }
        }
    }
}