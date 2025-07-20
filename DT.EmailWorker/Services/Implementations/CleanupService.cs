using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// Implementation of cleanup and archival service
    /// </summary>
    public class CleanupService : ICleanupService
    {
        private readonly EmailDbContext _context;
        private readonly CleanupSettings _settings;
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(
            EmailDbContext context,
            IOptions<CleanupSettings> settings,
            ILogger<CleanupService> logger)
        {
            _context = context;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<int> CleanupEmailHistoryAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);
                var oldRecords = await _context.EmailHistory
                    .Where(h => h.SentAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync(cancellationToken);  // Also add cancellationToken here

                if (oldRecords.Any())
                {
                    _context.EmailHistory.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Cleaned up {Count} old email history records", oldRecords.Count);
                    return oldRecords.Count;
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up email history");
                throw;
            }
        }

        public async Task<int> CleanupProcessingLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);

                var oldLogs = await _context.ProcessingLogs
                    .Where(l => l.CreatedAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync(cancellationToken);

                if (oldLogs.Any())
                {
                    _context.ProcessingLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old processing logs", oldLogs.Count);
                    return oldLogs.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up processing logs");
                throw;
            }
        }

        public async Task<int> CleanupEmailAttachmentsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);

                var oldAttachments = await _context.EmailAttachments
                    .Where(a => a.CreatedAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync();

                if (oldAttachments.Any())
                {
                    _context.EmailAttachments.RemoveRange(oldAttachments);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old email attachments", oldAttachments.Count);
                    return oldAttachments.Count;
                }

                return 0;
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
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);

                var oldStatuses = await _context.ServiceStatus
                    .Where(s => s.UpdatedAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync();

                if (oldStatuses.Any())
                {
                    _context.ServiceStatus.RemoveRange(oldStatuses);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old service status records", oldStatuses.Count);
                    return oldStatuses.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up service status records");
                throw;
            }
        }

        public async Task<ArchiveResult> ArchiveEmailHistoryAsync(int retentionDays, string? archivePath = null)
        {
            try
            {
                var result = new ArchiveResult();
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);

                var recordsToArchive = await _context.EmailHistory
                    .Where(h => h.SentAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync();

                if (!recordsToArchive.Any())
                {
                    result.Success = true;
                    result.RecordsArchived = 0;
                    return result;
                }

                // Create archive file
                var fileName = $"EmailHistory_Archive_{DateTime.UtcNow.AddHours(3):yyyyMMdd_HHmmss}.json.gz";
                var filePath = Path.Combine(archivePath ?? _settings.BackupPath, fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                // Serialize and compress data
                var jsonData = JsonSerializer.Serialize(recordsToArchive, new JsonSerializerOptions { WriteIndented = true });

                using var fileStream = new FileStream(filePath, FileMode.Create);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
                using var writer = new StreamWriter(gzipStream);

                await writer.WriteAsync(jsonData);

                result.Success = true;
                result.RecordsArchived = recordsToArchive.Count;
                result.ArchiveFiles.Add(filePath);

                _logger.LogInformation("Archived {Count} email history records to {FilePath}",
                    recordsToArchive.Count, filePath);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving email history");
                return new ArchiveResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<int> CleanupFailedEmailsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);

                var failedEmails = await _context.EmailQueue
                    .Where(q => q.Status == EmailQueueStatus.Failed && q.UpdatedAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync();

                if (failedEmails.Any())
                {
                    _context.EmailQueue.RemoveRange(failedEmails);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old failed emails", failedEmails.Count);
                    return failedEmails.Count;
                }

                return 0;
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
                // Find attachments that don't have corresponding email queue or history records
                var orphanedAttachments = await _context.EmailAttachments
                    .Where(a => !_context.EmailQueue.Any(q => q.QueueId == a.QueueId) &&
                               !_context.EmailHistory.Any(h => h.QueueId == a.QueueId))
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync();

                if (orphanedAttachments.Any())
                {
                    _context.EmailAttachments.RemoveRange(orphanedAttachments);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} orphaned attachments", orphanedAttachments.Count);
                    return orphanedAttachments.Count;
                }

                return 0;
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
                CleanupStarted = DateTime.UtcNow.AddHours(3)
            };

            try
            {
                _logger.LogInformation("Starting full cleanup operation");

                // Cleanup email history
                summary.EmailHistoryRecordsCleaned = await CleanupEmailHistoryAsync(_settings.EmailHistoryRetentionDays);

                // Cleanup processing logs
                summary.ProcessingLogsCleaned = await CleanupProcessingLogsAsync(_settings.ProcessingLogsRetentionDays);

                // Cleanup attachments
                summary.AttachmentsCleaned = await CleanupEmailAttachmentsAsync(_settings.RetentionDays);

                // Cleanup service status
                summary.ServiceStatusRecordsCleaned = await CleanupServiceStatusAsync(_settings.ServiceStatusRetentionDays);

                // Cleanup failed emails
                summary.FailedEmailsCleaned = await CleanupFailedEmailsAsync(_settings.FailedEmailsRetentionDays);

                // Cleanup orphaned attachments
                summary.OrphanedAttachmentsCleaned = await CleanupOrphanedAttachmentsAsync();

                // Optimize database if enabled
                if (_settings.OptimizeDatabaseAfterCleanup)
                {
                    summary.DatabaseOptimized = await OptimizeDatabaseAsync();
                }

                summary.CleanupCompleted = DateTime.UtcNow.AddHours(3);
                summary.TotalProcessingTime = summary.CleanupCompleted - summary.CleanupStarted;

                _logger.LogInformation("Full cleanup completed. Records cleaned: History={EmailHistory}, Logs={ProcessingLogs}, Attachments={Attachments}, Failed={Failed}, Orphaned={Orphaned}",
                    summary.EmailHistoryRecordsCleaned, summary.ProcessingLogsCleaned, summary.AttachmentsCleaned,
                    summary.FailedEmailsCleaned, summary.OrphanedAttachmentsCleaned);

                return summary;
            }
            catch (Exception ex)
            {
                summary.Errors.Add($"Full cleanup failed: {ex.Message}");
                _logger.LogError(ex, "Error during full cleanup operation");
                throw;
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
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS EmailAttachments");
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS ProcessingLogs");

                // Rebuild indexes if needed (optional - can be resource intensive)
                if (_settings.EnableAggressiveCleanup)
                {
                    await _context.Database.ExecuteSqlRawAsync("ALTER INDEX ALL ON EmailQueue REORGANIZE");
                    await _context.Database.ExecuteSqlRawAsync("ALTER INDEX ALL ON EmailHistory REORGANIZE");
                    await _context.Database.ExecuteSqlRawAsync("ALTER INDEX ALL ON EmailAttachments REORGANIZE");
                    await _context.Database.ExecuteSqlRawAsync("ALTER INDEX ALL ON ProcessingLogs REORGANIZE");
                }

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
                var now = DateTime.UtcNow.AddHours(3);
                var thirtyDaysAgo = now.AddDays(-30);
                var ninetyDaysAgo = now.AddDays(-90);
                var oneEightyDaysAgo = now.AddDays(-180);

                var stats = new CleanupStatistics
                {
                    TotalEmailHistoryRecords = await _context.EmailHistory.CountAsync(),
                    TotalProcessingLogs = await _context.ProcessingLogs.CountAsync(),
                    TotalAttachments = await _context.EmailAttachments.CountAsync(),
                    TotalServiceStatusRecords = await _context.ServiceStatus.CountAsync(),
                    RecordsOlderThan30Days = await _context.EmailHistory.CountAsync(h => h.SentAt < thirtyDaysAgo),
                    RecordsOlderThan90Days = await _context.EmailHistory.CountAsync(h => h.SentAt < ninetyDaysAgo),
                    RecordsOlderThan180Days = await _context.EmailHistory.CountAsync(h => h.SentAt < oneEightyDaysAgo)
                };

                // Get oldest record
                var oldestHistory = await _context.EmailHistory
                    .OrderBy(h => h.SentAt)
                    .FirstOrDefaultAsync();

                if (oldestHistory != null)
                {
                    stats.OldestRecord = oldestHistory.SentAt ?? DateTime.UtcNow.AddHours(3); // Handle nullable DateTime
                }

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
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);

                var estimate = new CleanupEstimate
                {
                    EmailHistoryRecordsToClean = await _context.EmailHistory.CountAsync(h => h.SentAt < cutoffDate),
                    ProcessingLogsToClean = await _context.ProcessingLogs.CountAsync(l => l.CreatedAt < cutoffDate),
                    AttachmentsToClean = await _context.EmailAttachments.CountAsync(a => a.CreatedAt < cutoffDate)
                };

                // Calculate estimated processing time (rough estimate)
                var totalRecords = estimate.EmailHistoryRecordsToClean + estimate.ProcessingLogsToClean + estimate.AttachmentsToClean;
                estimate.EstimatedProcessingTime = TimeSpan.FromSeconds(totalRecords * 0.001); // ~1ms per record

                // Add recommendations
                if (estimate.EmailHistoryRecordsToClean > 10000)
                {
                    estimate.Recommendations.Add("Consider creating a backup before cleanup due to large number of records");
                }

                if (retentionDays < 30)
                {
                    estimate.Warnings.Add("Retention period is less than 30 days - this may delete recent data");
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
            try
            {
                var result = new BackupResult();
                var startTime = DateTime.UtcNow.AddHours(3);

                var fileName = $"EmailWorker_Backup_{DateTime.UtcNow.AddHours(3):yyyyMMdd_HHmmss}.bak";
                var filePath = Path.Combine(backupPath ?? _settings.BackupPath, fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                // Get connection string
                var connectionString = _context.Database.GetConnectionString();
                var databaseName = _context.Database.GetDbConnection().Database;

                // Create backup using SQL command
                var backupSql = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = '{filePath}' 
                    WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                await _context.Database.ExecuteSqlRawAsync(backupSql);

                result.Success = true;
                result.BackupFilePath = filePath;
                result.ProcessingTime = DateTime.UtcNow.AddHours(3) - startTime;

                // Get file size
                if (File.Exists(filePath))
                {
                    result.BackupFileSizeBytes = new FileInfo(filePath).Length;
                }

                _logger.LogInformation("Database backup created successfully at {FilePath}", filePath);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database backup");
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<int> CleanupOldBackupsAsync(int retentionDays)
        {
            try
            {
                var backupDirectory = new DirectoryInfo(_settings.BackupPath);
                if (!backupDirectory.Exists)
                {
                    return 0;
                }

                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);
                var oldBackups = backupDirectory.GetFiles("*.bak")
                    .Where(f => f.CreationTime < cutoffDate)
                    .ToList();

                foreach (var backup in oldBackups)
                {
                    backup.Delete();
                    _logger.LogInformation("Deleted old backup file: {FileName}", backup.Name);
                }

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

                // 🚀 FIX: Use FormattableString (notice the $ prefix)
                var dbSizeResult = await _context.Database
                    .SqlQuery<decimal>($@"
                SELECT 
                    SUM(size * 8.0 / 1024 / 1024) as Value
                FROM sys.master_files 
                WHERE database_id = DB_ID()")
                    .FirstOrDefaultAsync();

                analysis.DatabaseSizeBytes = (long)(dbSizeResult * 1024 * 1024 * 1024);

                // Get drive info
                var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory)!);
                analysis.FreeDiskSpaceBytes = drive.AvailableFreeSpace;
                analysis.TotalDiskSpaceBytes = drive.TotalSize;
                analysis.UsedDiskSpaceBytes = drive.TotalSize - drive.AvailableFreeSpace;
                analysis.FreeSpacePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                analysis.UsedSpacePercent = 100 - analysis.FreeSpacePercent;
                analysis.DatabaseSpacePercent = (double)analysis.DatabaseSizeBytes / drive.TotalSize * 100;

                // 🚀 FIX: Use KSA time correctly
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-_settings.RetentionDays);
                var oldRecordsCount = await _context.EmailHistory.CountAsync(h => h.SentAt < cutoffDate);

                // Rough estimate: 1KB per email history record
                analysis.EstimatedReclaimableBytes = oldRecordsCount * 1024;

                // Determine if action needed
                analysis.IsLowOnSpace = analysis.FreeSpacePercent < 20; // Less than 20% free
                analysis.RequiresCleanup = analysis.FreeSpacePercent < 10; // Less than 10% free

                // Add recommendations
                if (analysis.FreeSpacePercent < 5)
                {
                    analysis.Recommendations.Add("CRITICAL: Immediate cleanup required - less than 5% disk space remaining");
                    analysis.Recommendations.Add("Perform aggressive cleanup to free space immediately");
                }
                else if (analysis.FreeSpacePercent < 10)
                {
                    analysis.Recommendations.Add("WARNING: Low disk space - cleanup recommended");
                    analysis.Recommendations.Add("Consider archiving old email history records");
                }
                else if (analysis.FreeSpacePercent < 20)
                {
                    analysis.Recommendations.Add("Consider routine cleanup to maintain performance");
                }

                if (oldRecordsCount > 1000)
                {
                    analysis.Recommendations.Add($"Found {oldRecordsCount} old email records that can be cleaned up");
                }

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing disk space");

                // 🚀 FIX: Return a fallback analysis instead of throwing
                return new DiskSpaceAnalysis
                {
                    DatabaseSizeBytes = 0,
                    FreeDiskSpaceBytes = 0,
                    TotalDiskSpaceBytes = 0,
                    UsedDiskSpaceBytes = 0,
                    FreeSpacePercent = 0,
                    UsedSpacePercent = 0,
                    DatabaseSpacePercent = 0,
                    EstimatedReclaimableBytes = 0,
                    IsLowOnSpace = false,
                    RequiresCleanup = false,
                    Recommendations = new List<string> { "Unable to analyze disk space - check logs for details" }
                };
            }
        }




        public async Task<CleanupSummary> PerformAggressiveCleanupAsync(int targetFreeSpacePercent)
        {
            var summary = new CleanupSummary
            {
                CleanupStarted = DateTime.UtcNow.AddHours(3)
            };

            try
            {
                _logger.LogInformation("Starting aggressive cleanup operation with {TargetFreeSpacePercent}% target free space", targetFreeSpacePercent);

                // Calculate retention days based on target free space
                int retentionDays = targetFreeSpacePercent > 50 ? 30 : targetFreeSpacePercent > 20 ? 7 : 1;

                // Create backup first if enabled
                if (_settings.CreateBackupBeforeCleanup)
                {
                    var backupResult = await CreateBackupAsync();
                    if (!backupResult.Success)
                    {
                        summary.Warnings.Add($"Backup failed: {backupResult.ErrorMessage}");
                    }
                }

                // Perform all cleanup operations with reduced retention
                summary.EmailHistoryRecordsCleaned = await CleanupEmailHistoryAsync(retentionDays);
                summary.ProcessingLogsCleaned = await CleanupProcessingLogsAsync(retentionDays);
                summary.AttachmentsCleaned = await CleanupEmailAttachmentsAsync(retentionDays);
                summary.ServiceStatusRecordsCleaned = await CleanupServiceStatusAsync(retentionDays);
                summary.FailedEmailsCleaned = await CleanupFailedEmailsAsync(retentionDays);
                summary.OrphanedAttachmentsCleaned = await CleanupOrphanedAttachmentsAsync();

                // Clean up old backups
                await CleanupOldBackupsAsync(_settings.BackupRetentionDays);

                // Force database optimization
                summary.DatabaseOptimized = await OptimizeDatabaseAsync();

                summary.CleanupCompleted = DateTime.UtcNow.AddHours(3);
                summary.TotalProcessingTime = summary.CleanupCompleted - summary.CleanupStarted;

                _logger.LogInformation("Aggressive cleanup completed in {Duration}", summary.TotalProcessingTime);

                return summary;
            }
            catch (Exception ex)
            {
                summary.Errors.Add($"Aggressive cleanup failed: {ex.Message}");
                _logger.LogError(ex, "Error during aggressive cleanup operation");
                throw;
            }
        }
    }

    /// <summary>
    /// Disk space analysis result - REMOVED DUPLICATE - this should be in ICleanupService.cs
    /// </summary>
}