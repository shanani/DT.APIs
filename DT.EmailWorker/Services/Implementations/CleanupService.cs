using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// Implementation of cleanup and archival service
    /// </summary>
    public class CleanupService : ICleanupService
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(EmailDbContext context, ILogger<CleanupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CleanupEmailHistoryAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var oldRecords = await _context.EmailHistory
                    .Where(h => h.CreatedAt < cutoffDate)
                    .ToListAsync();

                _context.EmailHistory.RemoveRange(oldRecords);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} email history records older than {Days} days",
                    oldRecords.Count, retentionDays);

                return oldRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up email history");
                throw;
            }
        }

        public async Task<int> CleanupProcessingLogsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var oldLogs = await _context.ProcessingLogs
                    .Where(l => l.CreatedAt < cutoffDate)
                    .ToListAsync();

                _context.ProcessingLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} processing logs older than {Days} days",
                    oldLogs.Count, retentionDays);

                return oldLogs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up processing logs");
                throw;
            }
        }

        public async Task<int> CleanupEmailAttachmentsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var oldAttachments = await _context.EmailAttachments
                    .Where(a => a.CreatedAt < cutoffDate)
                    .ToListAsync();

                _context.EmailAttachments.RemoveRange(oldAttachments);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} email attachments older than {Days} days",
                    oldAttachments.Count, retentionDays);

                return oldAttachments.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up email attachments");
                throw;
            }
        }

        public async Task<int> CleanupServiceStatusAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var oldStatusRecords = await _context.ServiceStatus
                    .Where(s => s.UpdatedAt < cutoffDate)
                    .ToListAsync();

                _context.ServiceStatus.RemoveRange(oldStatusRecords);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} service status records older than {Days} days",
                    oldStatusRecords.Count, retentionDays);

                return oldStatusRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up service status records");
                throw;
            }
        }

        public async Task<ArchiveResult> ArchiveEmailHistoryAsync(int retentionDays, string? archivePath = null)
        {
            var result = new ArchiveResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var recordsToArchive = await _context.EmailHistory
                    .Where(h => h.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (!recordsToArchive.Any())
                {
                    result.Success = true;
                    result.RecordsArchived = 0;
                    return result;
                }

                // Create archive directory if not specified
                archivePath ??= Path.Combine(Path.GetTempPath(), "EmailWorkerArchives");
                Directory.CreateDirectory(archivePath);

                var archiveFileName = $"EmailHistory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                var archiveFilePath = Path.Combine(archivePath, archiveFileName);

                // Serialize records to JSON
                var jsonData = System.Text.Json.JsonSerializer.Serialize(recordsToArchive, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(archiveFilePath, jsonData);

                // Remove archived records from database
                _context.EmailHistory.RemoveRange(recordsToArchive);
                await _context.SaveChangesAsync();

                stopwatch.Stop();

                result.Success = true;
                result.RecordsArchived = recordsToArchive.Count;
                result.ArchiveFilePath = archiveFilePath;
                result.ArchiveFileSizeBytes = new FileInfo(archiveFilePath).Length;
                result.ProcessingTime = stopwatch.Elapsed;
                result.ArchiveFiles.Add(archiveFilePath);

                _logger.LogInformation("Archived {Count} email history records to {FilePath} ({SizeMB:F1} MB)",
                    result.RecordsArchived, archiveFilePath, result.ArchiveFileSizeBytes / 1024.0 / 1024.0);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ProcessingTime = stopwatch.Elapsed;

                _logger.LogError(ex, "Error archiving email history");
                return result;
            }
        }

        public async Task<int> CleanupFailedEmailsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var failedEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Failed && e.CreatedAt < cutoffDate)
                    .ToListAsync();

                _context.EmailQueue.RemoveRange(failedEmails);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} failed emails older than {Days} days",
                    failedEmails.Count, retentionDays);

                return failedEmails.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up failed emails");
                throw;
            }
        }

        public async Task<int> CleanupOrphanedAttachmentsAsync()
        {
            try
            {
                // Find attachments that don't have corresponding emails in queue or history
                var orphanedAttachments = await _context.EmailAttachments
                    .Where(a => !_context.EmailQueue.Any(q => q.QueueId == a.QueueId) &&
                               !_context.EmailHistory.Any(h => h.QueueId == a.QueueId))
                    .ToListAsync();

                _context.EmailAttachments.RemoveRange(orphanedAttachments);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} orphaned attachments", orphanedAttachments.Count);

                return orphanedAttachments.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned attachments");
                throw;
            }
        }

        public async Task<CleanupSummary> PerformFullCleanupAsync()
        {
            var summary = new CleanupSummary
            {
                CleanupStarted = DateTime.UtcNow
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting full cleanup operation");

                // Cleanup email history (6 months)
                try
                {
                    summary.EmailHistoryRecordsCleaned = await CleanupEmailHistoryAsync(180);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Email history cleanup failed: {ex.Message}");
                }

                // Cleanup processing logs (30 days)
                try
                {
                    summary.ProcessingLogsCleaned = await CleanupProcessingLogsAsync(30);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Processing logs cleanup failed: {ex.Message}");
                }

                // Cleanup attachments (90 days)
                try
                {
                    summary.AttachmentsCleaned = await CleanupEmailAttachmentsAsync(90);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Attachments cleanup failed: {ex.Message}");
                }

                // Cleanup service status (7 days)
                try
                {
                    summary.ServiceStatusRecordsCleaned = await CleanupServiceStatusAsync(7);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Service status cleanup failed: {ex.Message}");
                }

                // Cleanup failed emails (7 days)
                try
                {
                    summary.FailedEmailsCleaned = await CleanupFailedEmailsAsync(7);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Failed emails cleanup failed: {ex.Message}");
                }

                // Cleanup orphaned attachments
                try
                {
                    summary.OrphanedAttachmentsCleaned = await CleanupOrphanedAttachmentsAsync();
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Orphaned attachments cleanup failed: {ex.Message}");
                }

                // Optimize database
                try
                {
                    summary.DatabaseOptimized = await OptimizeDatabaseAsync();
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Database optimization failed: {ex.Message}");
                    summary.DatabaseOptimized = false;
                }

                stopwatch.Stop();
                summary.CleanupCompleted = DateTime.UtcNow;
                summary.TotalProcessingTime = stopwatch.Elapsed;

                var totalCleaned = summary.EmailHistoryRecordsCleaned + summary.ProcessingLogsCleaned +
                                  summary.AttachmentsCleaned + summary.ServiceStatusRecordsCleaned +
                                  summary.FailedEmailsCleaned + summary.OrphanedAttachmentsCleaned;

                _logger.LogInformation("Full cleanup completed: {TotalRecords} records cleaned in {ProcessingTime}",
                    totalCleaned, stopwatch.Elapsed);

                return summary;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                summary.CleanupCompleted = DateTime.UtcNow;
                summary.TotalProcessingTime = stopwatch.Elapsed;
                summary.Errors.Add($"Full cleanup failed: {ex.Message}");

                _logger.LogError(ex, "Error during full cleanup operation");
                return summary;
            }
        }

        public async Task<bool> OptimizeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database optimization");

                // Update statistics
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS EmailQueue");
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS EmailHistory");
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS ProcessingLogs");

                // Rebuild indexes if fragmentation is high (simplified version)
                await _context.Database.ExecuteSqlRawAsync(@"
                    DECLARE @sql NVARCHAR(MAX) = '';
                    SELECT @sql = @sql + 'ALTER INDEX ' + i.name + ' ON ' + t.name + ' REBUILD;' + CHAR(13)
                    FROM sys.indexes i
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    WHERE t.name IN ('EmailQueue', 'EmailHistory', 'ProcessingLogs')
                    AND i.type > 0;
                    EXEC sp_executesql @sql;");

                _logger.LogInformation("Database optimization completed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing database");
                return false;
            }
        }

        public async Task<CleanupStatistics> GetCleanupStatisticsAsync()
        {
            try
            {
                var stats = new CleanupStatistics();

                // Get record counts
                stats.TotalEmailHistoryRecords = await _context.EmailHistory.CountAsync();
                stats.TotalProcessingLogs = await _context.ProcessingLogs.CountAsync();
                stats.TotalAttachments = await _context.EmailAttachments.CountAsync();
                stats.TotalServiceStatusRecords = await _context.ServiceStatus.CountAsync();

                // Get age-based counts
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
                var oneEightyDaysAgo = DateTime.UtcNow.AddDays(-180);

                stats.RecordsOlderThan30Days = await _context.EmailHistory.CountAsync(h => h.CreatedAt < thirtyDaysAgo);
                stats.RecordsOlderThan90Days = await _context.EmailHistory.CountAsync(h => h.CreatedAt < ninetyDaysAgo);
                stats.RecordsOlderThan180Days = await _context.EmailHistory.CountAsync(h => h.CreatedAt < oneEightyDaysAgo);

                // Get oldest record
                var oldestRecord = await _context.EmailHistory
                    .OrderBy(h => h.CreatedAt)
                    .Select(h => h.CreatedAt)
                    .FirstOrDefaultAsync();

                stats.OldestRecord = oldestRecord;

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cleanup statistics");
                throw;
            }
        }

        public async Task<CleanupEstimate> EstimateCleanupImpactAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var estimate = new CleanupEstimate();

                // Estimate records to clean
                estimate.EmailHistoryRecordsToClean = await _context.EmailHistory.CountAsync(h => h.CreatedAt < cutoffDate);
                estimate.ProcessingLogsToClean = await _context.ProcessingLogs.CountAsync(l => l.CreatedAt < cutoffDate);
                estimate.AttachmentsToClean = await _context.EmailAttachments.CountAsync(a => a.CreatedAt < cutoffDate);

                // Rough space estimation (this would need more sophisticated calculation in production)
                estimate.EstimatedSpaceToFreeBytes = (estimate.EmailHistoryRecordsToClean * 2000) + // ~2KB per history record
                                                    (estimate.ProcessingLogsToClean * 500) +       // ~500B per log
                                                    (estimate.AttachmentsToClean * 50000);       // ~50KB per attachment

                // Estimated processing time (rough calculation)
                var totalRecords = estimate.EmailHistoryRecordsToClean + estimate.ProcessingLogsToClean + estimate.AttachmentsToClean;
                estimate.EstimatedProcessingTime = TimeSpan.FromSeconds(totalRecords / 1000.0); // ~1000 records per second

                // Add recommendations
                if (estimate.EmailHistoryRecordsToClean > 100000)
                {
                    estimate.Recommendations.Add("Consider archiving instead of deleting due to large volume");
                }

                if (estimate.EstimatedSpaceToFreeBytes > 1024 * 1024 * 1024) // > 1GB
                {
                    estimate.Recommendations.Add("Significant space will be freed - ensure backup before cleanup");
                }

                return estimate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating cleanup impact");
                throw;
            }
        }

        public async Task<BackupResult> CreateBackupAsync(string? backupPath = null)
        {
            var result = new BackupResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                backupPath ??= Path.Combine(Path.GetTempPath(), "EmailWorkerBackups");
                Directory.CreateDirectory(backupPath);

                var backupFileName = $"EmailWorker_Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql";
                var fullBackupPath = Path.Combine(backupPath, backupFileName);

                // This is a simplified backup - in production you'd use SQL Server backup commands
                var connectionString = _context.Database.GetConnectionString();
                var databaseName = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString).InitialCatalog;

                await _context.Database.ExecuteSqlRawAsync($@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = '{fullBackupPath}' 
                    WITH FORMAT, INIT");

                stopwatch.Stop();

                result.Success = true;
                result.BackupFilePath = fullBackupPath;
                result.BackupFileSizeBytes = new FileInfo(fullBackupPath).Length;
                result.ProcessingTime = stopwatch.Elapsed;
                result.BackupCreated = DateTime.UtcNow;
                result.BackupType = "Full";

                _logger.LogInformation("Database backup created: {BackupPath} ({SizeMB:F1} MB)",
                    fullBackupPath, result.BackupFileSizeBytes / 1024.0 / 1024.0);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ProcessingTime = stopwatch.Elapsed;

                _logger.LogError(ex, "Error creating backup");
                return result;
            }
        }

        public async Task<int> CleanupOldBackupsAsync(int retentionDays)
        {
            try
            {
                var backupPath = Path.Combine(Path.GetTempPath(), "EmailWorkerBackups");
                if (!Directory.Exists(backupPath))
                    return 0;

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var oldBackups = Directory.GetFiles(backupPath, "EmailWorker_Backup_*.sql")
                    .Where(f => File.GetCreationTime(f) < cutoffDate)
                    .ToList();

                foreach (var backup in oldBackups)
                {
                    File.Delete(backup);
                }

                _logger.LogInformation("Cleaned up {Count} old backup files", oldBackups.Count);
                return oldBackups.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old backups");
                throw;
            }
        }

        public async Task<DiskSpaceAnalysis> AnalyzeDiskSpaceAsync()
        {
            try
            {
                var analysis = new DiskSpaceAnalysis();

                // Get disk space information
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                var systemDrive = drives.FirstOrDefault(d => d.Name.StartsWith("C:")) ?? drives.First();

                analysis.TotalDiskSpaceBytes = systemDrive.TotalSize;
                analysis.FreeDiskSpaceBytes = systemDrive.AvailableFreeSpace;
                analysis.UsedDiskSpaceBytes = analysis.TotalDiskSpaceBytes - analysis.FreeDiskSpaceBytes;
                analysis.FreeSpacePercent = (double)analysis.FreeDiskSpaceBytes / analysis.TotalDiskSpaceBytes * 100;
                analysis.UsedSpacePercent = 100 - analysis.FreeSpacePercent;

                // Estimate database size (simplified)
                var stats = await GetCleanupStatisticsAsync();
                analysis.DatabaseSizeBytes = stats.TotalEmailHistoryRecords * 2000L +
                                           stats.TotalProcessingLogs * 500L +
                                           stats.TotalAttachments * 50000L;
                analysis.DatabaseSpacePercent = (double)analysis.DatabaseSizeBytes / analysis.TotalDiskSpaceBytes * 100;

                // Determine if action is needed
                analysis.IsLowOnSpace = analysis.FreeSpacePercent < 15; // Less than 15% free
                analysis.RequiresCleanup = analysis.FreeSpacePercent < 25 || stats.RecordsOlderThan180Days > 10000;

                // Estimate reclaimable space
                analysis.EstimatedReclaimableBytes = stats.RecordsOlderThan180Days * 2000L +
                                                   stats.RecordsOlderThan30Days * 500L; // Rough estimate

                // Add recommendations
                if (analysis.IsLowOnSpace)
                {
                    analysis.Recommendations.Add("Critical: Disk space is low, immediate cleanup recommended");
                }
                if (analysis.RequiresCleanup)
                {
                    analysis.Recommendations.Add("Cleanup recommended to free space and improve performance");
                }
                if (analysis.EstimatedReclaimableBytes > 100 * 1024 * 1024) // > 100MB
                {
                    analysis.Recommendations.Add($"Approximately {analysis.EstimatedReclaimableBytes / 1024 / 1024} MB can be reclaimed");
                }

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing disk space");
                throw;
            }
        }

        public async Task<CleanupSummary> PerformAggressiveCleanupAsync(int targetFreeSpacePercent)
        {
            var summary = new CleanupSummary
            {
                CleanupStarted = DateTime.UtcNow
            };

            try
            {
                _logger.LogWarning("Starting aggressive cleanup to reach {TargetPercent}% free space", targetFreeSpacePercent);

                // More aggressive retention periods
                // Cleanup email history (30 days instead of 180)
                try
                {
                    summary.EmailHistoryRecordsCleaned = await CleanupEmailHistoryAsync(30);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Aggressive email history cleanup failed: {ex.Message}");
                }

                // Cleanup processing logs (7 days instead of 30)
                try
                {
                    summary.ProcessingLogsCleaned = await CleanupProcessingLogsAsync(7);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Aggressive processing logs cleanup failed: {ex.Message}");
                }

                // Cleanup attachments (30 days instead of 90)
                try
                {
                    summary.AttachmentsCleaned = await CleanupEmailAttachmentsAsync(30);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Aggressive attachments cleanup failed: {ex.Message}");
                }

                // Cleanup all failed emails immediately
                try
                {
                    summary.FailedEmailsCleaned = await CleanupFailedEmailsAsync(0);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Aggressive failed emails cleanup failed: {ex.Message}");
                }

                // Cleanup all orphaned attachments
                try
                {
                    summary.OrphanedAttachmentsCleaned = await CleanupOrphanedAttachmentsAsync();
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Aggressive orphaned attachments cleanup failed: {ex.Message}");
                }

                // Cleanup all old service status records
                try
                {
                    summary.ServiceStatusRecordsCleaned = await CleanupServiceStatusAsync(1);
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Aggressive service status cleanup failed: {ex.Message}");
                }

                // Force database optimization
                try
                {
                    summary.DatabaseOptimized = await OptimizeDatabaseAsync();
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Database optimization failed: {ex.Message}");
                    summary.DatabaseOptimized = false;
                }

                summary.CleanupCompleted = DateTime.UtcNow;
                summary.TotalProcessingTime = summary.CleanupCompleted - summary.CleanupStarted;

                _logger.LogWarning("Aggressive cleanup completed in {ProcessingTime}", summary.TotalProcessingTime);

                return summary;
            }
            catch (Exception ex)
            {
                summary.CleanupCompleted = DateTime.UtcNow;
                summary.TotalProcessingTime = summary.CleanupCompleted - summary.CleanupStarted;
                summary.Errors.Add($"Aggressive cleanup failed: {ex.Message}");

                _logger.LogError(ex, "Error during aggressive cleanup operation");
                return summary;
            }
        }
    }
}