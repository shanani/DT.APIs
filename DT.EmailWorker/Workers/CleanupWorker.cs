using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DT.EmailWorker.Workers
{
    /// <summary>
    /// Background worker for cleanup and archival operations
    /// </summary>
    public class CleanupWorker : BackgroundService
    {
        private readonly ILogger<CleanupWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EmailWorkerSettings _settings;
        private readonly CleanupSettings _cleanupSettings;

        public CleanupWorker(
            ILogger<CleanupWorker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<EmailWorkerSettings> settings,
            IOptions<CleanupSettings> cleanupSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
            _cleanupSettings = cleanupSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.EnableAutoCleanup)
            {
                _logger.LogInformation("Auto cleanup is disabled, Cleanup Worker will not run");
                return;
            }

            _logger.LogInformation("Cleanup Worker starting");

            // Wait for the service to fully start before first cleanup
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check if it's time to run cleanup
                    if (IsCleanupTime())
                    {
                        await PerformCleanupAsync(stoppingToken);
                    }
                    else
                    {
                        // Check disk space more frequently
                        await CheckDiskSpaceAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cleanup Worker shutting down");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in cleanup worker");

                    // Log the error to health service
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();
                        await healthService.LogProcessingErrorAsync(null, "Cleanup worker error", ex.ToString(), "CleanupWorker");
                    }
                    catch
                    {
                        // Ignore errors in error logging
                    }

                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }

                // Wait for the configured interval (check every hour)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Cleanup Worker stopped");
        }

        private bool IsCleanupTime()
        {
            try
            {
                var now = DateTime.Now;
                var cleanupTime = TimeSpan.Parse(_cleanupSettings.CleanupTime);
                var currentTime = now.TimeOfDay;

                // Check if we're within 1 hour of the cleanup time
                var timeDiff = Math.Abs((currentTime - cleanupTime).TotalMinutes);
                return timeDiff <= 60;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cleanup time, defaulting to 2 AM");
                // Default to 2 AM if parsing fails
                return DateTime.Now.Hour == 2;
            }
        }

        private async Task PerformCleanupAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();
            var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();

            try
            {
                _logger.LogInformation("Starting scheduled cleanup operation");

                await healthService.LogProcessingInfoAsync(null, "Starting cleanup operation", "CleanupWorker", "CleanupStart");

                // Check disk space before cleanup
                var diskAnalysis = await cleanupService.AnalyzeDiskSpaceAsync();

                _logger.LogInformation("Disk space analysis: {FreePercent:F1}% free ({FreeGB:F1} GB available)",
                    diskAnalysis.FreeSpacePercent, diskAnalysis.FreeDiskSpaceBytes / 1024.0 / 1024.0 / 1024.0);

                CleanupSummary cleanupResult;

                // Perform aggressive cleanup if disk space is low
                if (diskAnalysis.IsLowOnSpace && _cleanupSettings.EnableAggressiveCleanup)
                {
                    _logger.LogWarning("Low disk space detected, performing aggressive cleanup");
                    cleanupResult = await cleanupService.PerformAggressiveCleanupAsync(_cleanupSettings.AggressiveCleanupThresholdPercent);

                    await healthService.SendHealthAlertAsync(AlertLevel.Warning,
                        "Aggressive cleanup performed due to low disk space",
                        new Dictionary<string, object>
                        {
                            ["FreeSpacePercent"] = diskAnalysis.FreeSpacePercent,
                            ["SpaceFreedMB"] = cleanupResult.SpaceFreedBytes / 1024 / 1024
                        });
                }
                else
                {
                    // Perform normal cleanup
                    cleanupResult = await cleanupService.PerformFullCleanupAsync();
                }

                // Log cleanup results
                _logger.LogInformation("Cleanup completed successfully. " +
                    "History records cleaned: {HistoryRecords}, " +
                    "Logs cleaned: {LogRecords}, " +
                    "Space freed: {SpaceFreedMB:F1} MB, " +
                    "Processing time: {ProcessingTime}",
                    cleanupResult.EmailHistoryRecordsCleaned,
                    cleanupResult.ProcessingLogsCleaned,
                    cleanupResult.SpaceFreedBytes / 1024.0 / 1024.0,
                    cleanupResult.TotalProcessingTime);

                await healthService.LogProcessingInfoAsync(null,
                    $"Cleanup completed: {cleanupResult.EmailHistoryRecordsCleaned} history, " +
                    $"{cleanupResult.ProcessingLogsCleaned} logs, " +
                    $"{cleanupResult.SpaceFreedBytes / 1024 / 1024} MB freed",
                    "CleanupWorker", "CleanupComplete");

                // Send cleanup report if configured
                if (_cleanupSettings.SendCleanupReports && _cleanupSettings.CleanupReportRecipients.Any())
                {
                    await SendCleanupReportAsync(cleanupResult, diskAnalysis);
                }

                // Log warnings if any
                if (cleanupResult.Warnings.Any())
                {
                    foreach (var warning in cleanupResult.Warnings)
                    {
                        _logger.LogWarning("Cleanup warning: {Warning}", warning);
                    }
                }

                // Log errors if any
                if (cleanupResult.Errors.Any())
                {
                    foreach (var error in cleanupResult.Errors)
                    {
                        _logger.LogError("Cleanup error: {Error}", error);
                        await healthService.LogProcessingErrorAsync(null, $"Cleanup error: {error}", null, "CleanupWorker");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup operation");
                await healthService.LogProcessingErrorAsync(null, "Cleanup operation failed", ex.ToString(), "CleanupWorker");
                throw;
            }
        }

        private async Task CheckDiskSpaceAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();
            var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();

            try
            {
                var diskAnalysis = await cleanupService.AnalyzeDiskSpaceAsync();

                // Send alert if disk space is critically low
                if (diskAnalysis.FreeSpacePercent < 5) // Less than 5% free
                {
                    await healthService.SendHealthAlertAsync(AlertLevel.Critical,
                        $"Critical disk space warning: Only {diskAnalysis.FreeSpacePercent:F1}% free",
                        new Dictionary<string, object>
                        {
                            ["FreeSpacePercent"] = diskAnalysis.FreeSpacePercent,
                            ["FreeSpaceGB"] = diskAnalysis.FreeDiskSpaceBytes / 1024.0 / 1024.0 / 1024.0,
                            ["Recommendations"] = diskAnalysis.Recommendations
                        });
                }
                else if (diskAnalysis.RequiresCleanup)
                {
                    _logger.LogInformation("Disk space monitoring: {FreePercent:F1}% free, cleanup recommended",
                        diskAnalysis.FreeSpacePercent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking disk space");
            }
        }

        private async Task SendCleanupReportAsync(CleanupSummary cleanupResult, DiskSpaceAnalysis diskAnalysis)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

                var reportSubject = $"Email Worker Cleanup Report - {DateTime.Now:yyyy-MM-dd}";
                var reportBody = $@"
                    <h2>Email Worker Cleanup Report</h2>
                    <p><strong>Cleanup Date:</strong> {cleanupResult.CleanupCompleted:yyyy-MM-dd HH:mm:ss}</p>
                    <p><strong>Processing Time:</strong> {cleanupResult.TotalProcessingTime}</p>
                    
                    <h3>Cleanup Results</h3>
                    <ul>
                        <li>Email History Records Cleaned: {cleanupResult.EmailHistoryRecordsCleaned:N0}</li>
                        <li>Processing Logs Cleaned: {cleanupResult.ProcessingLogsCleaned:N0}</li>
                        <li>Attachments Cleaned: {cleanupResult.AttachmentsCleaned:N0}</li>
                        <li>Failed Emails Cleaned: {cleanupResult.FailedEmailsCleaned:N0}</li>
                        <li>Orphaned Attachments Cleaned: {cleanupResult.OrphanedAttachmentsCleaned:N0}</li>
                        <li>Space Freed: {cleanupResult.SpaceFreedBytes / 1024.0 / 1024.0:F1} MB</li>
                        <li>Database Optimized: {(cleanupResult.DatabaseOptimized ? "Yes" : "No")}</li>
                    </ul>
                    
                    <h3>Disk Space Status</h3>
                    <ul>
                        <li>Free Space: {diskAnalysis.FreeSpacePercent:F1}% ({diskAnalysis.FreeDiskSpaceBytes / 1024.0 / 1024.0 / 1024.0:F1} GB)</li>
                        <li>Database Size: {diskAnalysis.DatabaseSizeBytes / 1024.0 / 1024.0 / 1024.0:F1} GB</li>
                        <li>Estimated Reclaimable: {diskAnalysis.EstimatedReclaimableBytes / 1024.0 / 1024.0:F1} MB</li>
                    </ul>";

                if (cleanupResult.Warnings.Any())
                {
                    reportBody += "<h3>Warnings</h3><ul>";
                    foreach (var warning in cleanupResult.Warnings)
                    {
                        reportBody += $"<li>{warning}</li>";
                    }
                    reportBody += "</ul>";
                }

                if (cleanupResult.Errors.Any())
                {
                    reportBody += "<h3>Errors</h3><ul>";
                    foreach (var error in cleanupResult.Errors)
                    {
                        reportBody += $"<li>{error}</li>";
                    }
                    reportBody += "</ul>";
                }

                // Queue cleanup report emails
                foreach (var recipient in _cleanupSettings.CleanupReportRecipients)
                {
                    var emailRequest = new Models.DTOs.EmailProcessingRequest
                    {
                        ToEmails = recipient,
                        Subject = reportSubject,
                        Body = reportBody,
                        IsHtml = true,
                        Priority = Models.Enums.EmailPriority.Low,
                        CreatedBy = "CleanupWorker",
                        RequestSource = "CleanupReport"
                    };

                    await queueService.QueueEmailAsync(emailRequest);
                }

                _logger.LogInformation("Cleanup report sent to {RecipientCount} recipients",
                    _cleanupSettings.CleanupReportRecipients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending cleanup report");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cleanup Worker stopping...");
            await base.StopAsync(stoppingToken);
        }
    }
}