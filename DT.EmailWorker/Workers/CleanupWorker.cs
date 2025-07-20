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
            // FIXED: Changed EnableAutoCleanup to use CleanupSettings instead of EmailWorkerSettings
            if (!_cleanupSettings.EnableAutoCleanup)
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
                    await PerformScheduledCleanupAsync();

                    // FIXED: Use CleanupIntervalHours instead of CleanupTime
                    var intervalHours = _cleanupSettings.CleanupIntervalHours;
                    var nextRunDelay = TimeSpan.FromHours(intervalHours);

                    _logger.LogInformation("Next cleanup scheduled in {Hours} hours", intervalHours);
                    await Task.Delay(nextRunDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is shutting down
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in cleanup worker execution");

                    // Wait a shorter interval on error before retrying
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }

        private async Task PerformScheduledCleanupAsync()
        {
            _logger.LogInformation("Starting scheduled cleanup operation");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();

                // Determine if aggressive cleanup is needed
                var isAggressiveCleanup = await ShouldPerformAggressiveCleanupAsync(scope.ServiceProvider);

                var cleanupResult = isAggressiveCleanup
                    ? await PerformAggressiveCleanupAsync(cleanupService)
                    : await PerformStandardCleanupAsync(cleanupService);

                // Log cleanup results
                LogCleanupResults(cleanupResult, isAggressiveCleanup);

                // Send cleanup report if enabled
                if (_cleanupSettings.SendCleanupReports)
                {
                    await SendCleanupReportAsync(scope.ServiceProvider, cleanupResult, isAggressiveCleanup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled cleanup operation");
                throw;
            }
        }

        private async Task<bool> ShouldPerformAggressiveCleanupAsync(IServiceProvider serviceProvider)
        {
            try
            {
                // Check if aggressive cleanup is enabled
                if (!_cleanupSettings.EnableAggressiveCleanup)
                {
                    return false;
                }

                var cleanupService = serviceProvider.GetRequiredService<ICleanupService>();
                var diskAnalysis = await cleanupService.AnalyzeDiskSpaceAsync();

                // REMOVED: AggressiveCleanupThresholdPercent doesn't exist in CleanupSettings
                // Using a hardcoded threshold or add the property to CleanupSettings if needed
                const double aggressiveThreshold = 85.0; // Default 85% disk usage threshold

                var diskUsagePercent = 100.0 - diskAnalysis.FreeSpacePercent;
                var shouldUseAggressive = diskUsagePercent >= aggressiveThreshold;

                if (shouldUseAggressive)
                {
                    _logger.LogWarning("Disk usage is {DiskUsage:F1}%, triggering aggressive cleanup", diskUsagePercent);
                }

                return shouldUseAggressive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if aggressive cleanup should be performed");
                return false;
            }
        }

        private async Task<CleanupResult> PerformStandardCleanupAsync(ICleanupService cleanupService)
        {
            var result = new CleanupResult
            {
                StartTime = DateTime.UtcNow.AddHours(3),
                IsAggressiveCleanup = false
            };

            try
            {
                // Standard cleanup operations using settings from CleanupSettings
                result.EmailHistoryRecordsDeleted = await cleanupService.CleanupEmailHistoryAsync(_cleanupSettings.EmailHistoryRetentionDays);
                result.ProcessingLogsDeleted = await cleanupService.CleanupProcessingLogsAsync(_cleanupSettings.ProcessingLogsRetentionDays);
                result.ServiceStatusRecordsDeleted = await cleanupService.CleanupServiceStatusAsync(_cleanupSettings.ServiceStatusRetentionDays);

                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow.AddHours(3);

                _logger.LogInformation("Standard cleanup completed successfully");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.EndTime = DateTime.UtcNow.AddHours(3);
                result.Errors.Add($"Standard cleanup failed: {ex.Message}");

                _logger.LogError(ex, "Standard cleanup failed");
                throw;
            }

            return result;
        }

        private async Task<CleanupResult> PerformAggressiveCleanupAsync(ICleanupService cleanupService)
        {
            var result = new CleanupResult
            {
                StartTime = DateTime.UtcNow.AddHours(3),
                IsAggressiveCleanup = true
            };

            try
            {
                // More aggressive retention periods
                var aggressiveEmailHistoryDays = Math.Min(_cleanupSettings.EmailHistoryRetentionDays, 30);
                var aggressiveLogsDays = Math.Min(_cleanupSettings.ProcessingLogsRetentionDays, 7);
                var aggressiveStatusDays = Math.Min(_cleanupSettings.ServiceStatusRetentionDays, 3);

                result.EmailHistoryRecordsDeleted = await cleanupService.CleanupEmailHistoryAsync(aggressiveEmailHistoryDays);
                result.ProcessingLogsDeleted = await cleanupService.CleanupProcessingLogsAsync(aggressiveLogsDays);
                result.ServiceStatusRecordsDeleted = await cleanupService.CleanupServiceStatusAsync(aggressiveStatusDays);

                // Additional aggressive cleanup operations
                result.AttachmentsDeleted = await cleanupService.CleanupEmailAttachmentsAsync(_cleanupSettings.FailedEmailsRetentionDays);

                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow.AddHours(3);

                _logger.LogInformation("Aggressive cleanup completed successfully");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.EndTime = DateTime.UtcNow.AddHours(3);
                result.Errors.Add($"Aggressive cleanup failed: {ex.Message}");

                _logger.LogError(ex, "Aggressive cleanup failed");
                throw;
            }

            return result;
        }

        private void LogCleanupResults(CleanupResult result, bool isAggressive)
        {
            var cleanupType = isAggressive ? "Aggressive" : "Standard";
            var duration = result.EndTime - result.StartTime;

            _logger.LogInformation(
                "{CleanupType} cleanup completed in {Duration:mm\\:ss} - " +
                "EmailHistory: {EmailHistory}, ProcessingLogs: {ProcessingLogs}, " +
                "ServiceStatus: {ServiceStatus}, Attachments: {Attachments}",
                cleanupType, duration,
                result.EmailHistoryRecordsDeleted,
                result.ProcessingLogsDeleted,
                result.ServiceStatusRecordsDeleted,
                result.AttachmentsDeleted);

            if (result.Warnings.Any())
            {
                foreach (var warning in result.Warnings)
                {
                    _logger.LogWarning("Cleanup warning: {Warning}", warning);
                }
            }

            if (result.Errors.Any())
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Cleanup error: {Error}", error);
                }
            }
        }

        private async Task SendCleanupReportAsync(IServiceProvider serviceProvider, CleanupResult cleanupResult, bool isAggressive)
        {
            try
            {
                var queueService = serviceProvider.GetRequiredService<IEmailQueueService>();
                var cleanupService = serviceProvider.GetRequiredService<ICleanupService>();

                var diskAnalysis = await cleanupService.AnalyzeDiskSpaceAsync();
                var duration = cleanupResult.EndTime - cleanupResult.StartTime;

                var reportSubject = $"Cleanup Report - {(isAggressive ? "Aggressive" : "Standard")} - {DateTime.UtcNow.AddHours(3):yyyy-MM-dd}";

                var reportBody = $@"
                    <h2>Email Worker Cleanup Report</h2>
                    <h3>Cleanup Summary</h3>
                    <ul>
                        <li>Type: {(isAggressive ? "Aggressive" : "Standard")} Cleanup</li>
                        <li>Status: {(cleanupResult.IsSuccess ? "Successful" : "Failed")}</li>
                        <li>Duration: {duration:mm\\:ss}</li>
                        <li>Started: {cleanupResult.StartTime:yyyy-MM-dd HH:mm:ss} UTC</li>
                        <li>Completed: {cleanupResult.EndTime:yyyy-MM-dd HH:mm:ss} UTC</li>
                    </ul>
                    
                    <h3>Records Cleaned</h3>
                    <ul>
                        <li>Email History: {cleanupResult.EmailHistoryRecordsDeleted} records</li>
                        <li>Processing Logs: {cleanupResult.ProcessingLogsDeleted} records</li>
                        <li>Service Status: {cleanupResult.ServiceStatusRecordsDeleted} records</li>
                        <li>Attachments: {cleanupResult.AttachmentsDeleted} files</li>
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

    /// <summary>
    /// Result of a cleanup operation
    /// </summary>
    public class CleanupResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsAggressiveCleanup { get; set; }
        public int EmailHistoryRecordsDeleted { get; set; }
        public int ProcessingLogsDeleted { get; set; }
        public int ServiceStatusRecordsDeleted { get; set; }
        public int AttachmentsDeleted { get; set; }
        public bool DatabaseOptimized { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Result of disk space analysis
    /// </summary>
    public class DiskAnalysis
    {
        public double FreeSpacePercent { get; set; }
        public long FreeDiskSpaceBytes { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public long EstimatedReclaimableBytes { get; set; }
    }
}