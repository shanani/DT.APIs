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
                var healthStatus = await healthService.GetServiceHealthAsync(cancellationToken);
                report.AppendLine($"Service Health: {healthStatus.HealthStatus}");
                report.AppendLine($"Uptime: {healthStatus.Uptime}");
                report.AppendLine($"Last Health Check: {healthStatus.LastHealthCheck:yyyy-MM-dd HH:mm:ss} UTC");
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
                var queueStats = await emailQueueService.GetQueueStatisticsAsync(cancellationToken);
                report.AppendLine("Queue Statistics:");
                report.AppendLine($"  Pending Emails: {queueStats.PendingCount:N0}");
                report.AppendLine($"  Processing Emails: {queueStats.ProcessingCount:N0}");
                report.AppendLine($"  Sent Emails: {queueStats.SentCount:N0}");
                report.AppendLine($"  Failed Emails: {queueStats.FailedCount:N0}");
                report.AppendLine($"  Total Emails: {queueStats.TotalCount:N0}");

                if (queueStats.TotalCount > 0)
                {
                    var successRate = (double)queueStats.SentCount / queueStats.TotalCount * 100;
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
                var metrics = await healthService.GetPerformanceMetricsAsync(cancellationToken);
                report.AppendLine("Performance Metrics (Last 24 Hours):");
                report.AppendLine($"  Emails Processed: {metrics.EmailsProcessed:N0}");
                report.AppendLine($"  Average Processing Time: {metrics.AverageProcessingTimeMs:F2}ms");
                report.AppendLine($"  Peak Processing Rate: {metrics.PeakProcessingRate:N0} emails/hour");
                report.AppendLine();
            }
            catch (Exception ex)
            {
                report.AppendLine($"Performance Metrics: ERROR - {ex.Message}");
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
                var emailQueue = new Models.Entities.EmailQueue
                {
                    ToEmails = _settings.StatusReportEmail!,
                    Subject = $"DT.EmailWorker Status Report - {DateTime.UtcNow:yyyy-MM-dd}",
                    Body = $"<pre>{reportContent}</pre>",
                    IsHtml = true,
                    Priority = Models.Enums.EmailPriority.High,
                    CreatedBy = "StatusReportWorker",
                    Status = Models.Enums.EmailQueueStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await emailQueueService.AddEmailToQueueAsync(emailQueue, cancellationToken);
                _logger.LogInformation("Status report email queued to {Email}", _settings.StatusReportEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status report email");
            }
        }
    }
}