using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DT.EmailWorker.Workers
{
    /// <summary>
    /// Background worker for generating and sending service status reports
    /// </summary>
    public class StatusReportWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailWorkerSettings _settings;
        private readonly ILogger<StatusReportWorker> _logger;

        public StatusReportWorker(
            IServiceProvider serviceProvider,
            IOptions<EmailWorkerSettings> settings,
            ILogger<StatusReportWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Status Report Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateStatusReportAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while generating status report");
                }

                // Wait for 24 hours before next status report
                var delay = TimeSpan.FromHours(24);
                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("Status Report Worker stopped");
        }

        private async Task GenerateStatusReportAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            try
            {
                // Generate status report
                var statusReport = await GenerateReportContentAsync(healthService, emailQueueService, cancellationToken);

                // Log the status report
                _logger.LogInformation("Daily Status Report Generated:\n{StatusReport}", statusReport);

                // Optionally send status report via email if configured
                if (!string.IsNullOrEmpty(_settings.StatusReportEmail))
                {
                    await SendStatusReportEmailAsync(statusReport, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate status report");
            }
        }

        private async Task<string> GenerateReportContentAsync(
            IHealthService healthService,
            IEmailQueueService emailQueueService,
            CancellationToken cancellationToken)
        {
            var report = new System.Text.StringBuilder();
            var reportDate = DateTime.UtcNow;

            report.AppendLine($"DT.EmailWorker Daily Status Report - {reportDate:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine("=".PadRight(70, '='));
            report.AppendLine();

            // Service Health Status
            try
            {
                // FIXED: Changed GetServiceHealthAsync to GetServiceStatusAsync (which exists in the interface)
                var healthStatus = await healthService.GetServiceStatusAsync();
                report.AppendLine($"Service Health: {healthStatus.Status}");
                report.AppendLine($"Last Heartbeat: {healthStatus.LastHeartbeat:yyyy-MM-dd HH:mm:ss} UTC");
                report.AppendLine($"Queue Depth: {healthStatus.QueueDepth}");
                report.AppendLine($"Emails Processed/Hour: {healthStatus.EmailsProcessedPerHour}");
                report.AppendLine($"Error Rate: {healthStatus.ErrorRate:F2}%");

                // Calculate uptime if possible
                var uptime = DateTime.UtcNow - healthStatus.StartedAt;
                report.AppendLine($"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m");
                report.AppendLine();
            }
            catch (Exception ex)
            {
                report.AppendLine($"Health Status: ERROR - {ex.Message}");
                report.AppendLine();
            }

            // Queue Statistics
            try
            {
                // FIXED: Removed cancellationToken parameter as GetQueueStatisticsAsync doesn't accept it
                var queueStats = await emailQueueService.GetQueueStatisticsAsync();
                report.AppendLine("Queue Statistics:");
                report.AppendLine($"  Queued Emails: {queueStats.TotalQueued:N0}");
                report.AppendLine($"  Processing Emails: {queueStats.TotalProcessing:N0}");
                report.AppendLine($"  Sent Emails: {queueStats.SentCount:N0}");
                report.AppendLine($"  Failed Emails: {queueStats.TotalFailed:N0}");
                report.AppendLine($"  Scheduled Emails: {queueStats.TotalScheduled:N0}");

                var totalProcessed = queueStats.SentCount + queueStats.TotalFailed;
                if (totalProcessed > 0)
                {
                    var successRate = (double)queueStats.SentCount / totalProcessed * 100;
                    report.AppendLine($"  Success Rate: {successRate:F2}%");
                }
                report.AppendLine();
            }
            catch (Exception ex)
            {
                report.AppendLine($"Queue Statistics: ERROR - {ex.Message}");
                report.AppendLine();
            }

            // Performance Metrics
            try
            {
                // FIXED: Changed cancellationToken to 24 (hours parameter) as expected by the method
                var metrics = await healthService.GetPerformanceMetricsAsync(24);
                report.AppendLine("Performance Metrics (Last 24 Hours):");
                report.AppendLine($"  Total Emails Processed: {metrics.TotalEmailsProcessed:N0}");
                report.AppendLine($"  Total Emails Failed: {metrics.TotalEmailsFailed:N0}");
                report.AppendLine($"  Success Rate: {metrics.SuccessRate:F2}%");
                report.AppendLine($"  Average Processing Time: {metrics.AverageProcessingTimeMs:F2}ms");
                report.AppendLine($"  Peak Queue Depth: {metrics.PeakQueueDepth:N0}");
                report.AppendLine($"  Average Queue Depth: {metrics.AverageQueueDepth:N0}");
                report.AppendLine($"  Average Emails/Hour: {metrics.AverageEmailsPerHour:F1}");
                report.AppendLine();
            }
            catch (Exception ex)
            {
                report.AppendLine($"Performance Metrics: ERROR - {ex.Message}");
                report.AppendLine();
            }

            // System Resource Usage
            try
            {
                var resourceUsage = await healthService.GetResourceUsageAsync();
                report.AppendLine("System Resource Usage:");
                report.AppendLine($"  CPU Usage: {resourceUsage.CpuUsagePercent:F1}%");
                report.AppendLine($"  Memory Usage: {resourceUsage.MemoryUsageMB:F1} MB ({resourceUsage.MemoryUsagePercent:F1}%)");
                report.AppendLine($"  Disk Usage: {resourceUsage.DiskUsagePercent:F1}%");
                report.AppendLine($"  Process Count: {resourceUsage.ProcessCount}");
                report.AppendLine($"  Thread Count: {resourceUsage.ThreadCount}");
                report.AppendLine();
            }
            catch (Exception ex)
            {
                report.AppendLine($"System Resource Usage: ERROR - {ex.Message}");
                report.AppendLine();
            }

            // Recent Errors/Alerts
            try
            {
                var unhealthyServices = await healthService.GetUnhealthyServicesAsync(10);
                if (unhealthyServices.Any())
                {
                    report.AppendLine("Unhealthy Services:");
                    foreach (var service in unhealthyServices)
                    {
                        report.AppendLine($"  {service.ServiceName} on {service.MachineName} - Last heartbeat: {service.LastHeartbeat:yyyy-MM-dd HH:mm:ss}");
                    }
                    report.AppendLine();
                }
                else
                {
                    report.AppendLine("All Services: Healthy");
                    report.AppendLine();
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"Service Health Check: ERROR - {ex.Message}");
                report.AppendLine();
            }

            // System Information
            report.AppendLine("System Information:");
            report.AppendLine($"  Server: {Environment.MachineName}");
            report.AppendLine($"  OS: {Environment.OSVersion}");
            report.AppendLine($"  .NET Version: {Environment.Version}");
            report.AppendLine($"  Working Set: {Environment.WorkingSet / 1024 / 1024:N0} MB");
            report.AppendLine($"  Processor Count: {Environment.ProcessorCount}");
            report.AppendLine();

            report.AppendLine("Report generated by DT.EmailWorker");

            return report.ToString();
        }

        private async Task SendStatusReportEmailAsync(string reportContent, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            try
            {
                // Create email request instead of EmailQueue entity
                var emailRequest = new Models.DTOs.EmailProcessingRequest
                {
                    ToEmails = _settings.StatusReportEmail!,
                    Subject = $"DT.EmailWorker Status Report - {DateTime.UtcNow:yyyy-MM-dd}",
                    Body = $"<pre>{reportContent}</pre>",
                    IsHtml = true,
                    Priority = Models.Enums.EmailPriority.High,
                    CreatedBy = "StatusReportWorker",
                    RequestSource = "StatusReport"
                };

                // FIXED: Changed AddEmailToQueueAsync to QueueEmailAsync (which exists in the interface)
                var queueId = await emailQueueService.QueueEmailAsync(emailRequest);
                _logger.LogInformation("Status report email queued with ID {QueueId} to {Email}", queueId, _settings.StatusReportEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status report email");
            }
        }
    }
}