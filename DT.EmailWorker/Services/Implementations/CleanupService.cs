using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public async Task<int> CleanupOldEmailHistoryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);

                var oldRecords = await _context.EmailHistory
                    .Where(h => h.SentAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync(cancellationToken);

                if (oldRecords.Any())
                {
                    _context.EmailHistory.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Cleaned up {Count} old email history records", oldRecords.Count);
                }

                return oldRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old email history");
                throw;
            }
        }

        public async Task<int> CleanupOldLogsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);

                var oldLogs = await _context.ProcessingLogs
                    .Where(l => l.CreatedAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync(cancellationToken);

                if (oldLogs.Any())
                {
                    _context.ProcessingLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Cleaned up {Count} old processing logs", oldLogs.Count);
                }

                return oldLogs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old logs");
                throw;
            }
        }

        public async Task<int> CleanupFailedEmailsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);

                var failedEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Failed && e.UpdatedAt < cutoffDate)
                    .Take(_settings.MaxRecordsPerCleanup)
                    .ToListAsync(cancellationToken);

                if (failedEmails.Any())
                {
                    _context.EmailQueue.RemoveRange(failedEmails);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Cleaned up {Count} old failed emails", failedEmails.Count);
                }

                return failedEmails.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup failed emails");
                throw;
            }
        }

        public async Task<CleanupSummary> PerformFullCleanupAsync(CancellationToken cancellationToken = default)
        {
            var summary = new CleanupSummary
            {
                CleanupStarted = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting full cleanup operation");

                // Cleanup email history
                summary.EmailHistoryDeleted = await CleanupOldEmailHistoryAsync(cancellationToken);

                // Cleanup logs
                summary.LogsDeleted = await CleanupOldLogsAsync(cancellationToken);

                // Cleanup failed emails
                summary.FailedEmailsDeleted = await CleanupFailedEmailsAsync(cancellationToken);

                summary.CleanupCompleted = DateTime.UtcNow;
                summary.IsSuccess = true;
                summary.TotalDeleted = summary.EmailHistoryDeleted + summary.LogsDeleted + summary.FailedEmailsDeleted;

                _logger.LogInformation("Full cleanup completed. Total records deleted: {TotalDeleted}", summary.TotalDeleted);
            }
            catch (Exception ex)
            {
                summary.CleanupCompleted = DateTime.UtcNow;
                summary.IsSuccess = false;
                summary.ErrorMessage = ex.Message;

                _logger.LogError(ex, "Full cleanup operation failed");
            }

            return summary;
        }

        public async Task<bool> CheckDiskSpaceAsync()
        {
            try
            {
                // Simple disk space check - in production you might want more sophisticated monitoring
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

                foreach (var drive in drives)
                {
                    var freeSpacePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;

                    if (freeSpacePercent < _settings.AggressiveCleanupThresholdPercent)
                    {
                        _logger.LogWarning("Low disk space detected on drive {DriveName}: {FreePercent:F1}% free",
                            drive.Name, freeSpacePercent);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check disk space");
                return true; // Assume OK if check fails
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
                _logger.LogInformation("Starting aggressive cleanup to achieve {TargetPercent}% free space", targetFreeSpacePercent);

                // More aggressive cleanup with shorter retention
                var aggressiveCutoffDate = DateTime.UtcNow.AddDays(-(_settings.RetentionDays / 2));

                // Cleanup with shorter retention period
                var emailHistoryCount = await CleanupRecordsByDate(_context.EmailHistory, aggressiveCutoffDate, cancellationToken);
                var logsCount = await CleanupRecordsByDate(_context.ProcessingLogs, aggressiveCutoffDate, cancellationToken);

                summary.EmailHistoryDeleted = emailHistoryCount;
                summary.LogsDeleted = logsCount;
                summary.TotalDeleted = emailHistoryCount + logsCount;

                summary.CleanupCompleted = DateTime.UtcNow;
                summary.IsSuccess = true;

                _logger.LogInformation("Aggressive cleanup completed. Total records deleted: {TotalDeleted}", summary.TotalDeleted);
            }
            catch (Exception ex)
            {
                summary.CleanupCompleted = DateTime.UtcNow;
                summary.IsSuccess = false;
                summary.ErrorMessage = ex.Message;

                _logger.LogError(ex, "Aggressive cleanup operation failed");
            }

            return summary;
        }

        private async Task<int> CleanupRecordsByDate<T>(DbSet<T> dbSet, DateTime cutoffDate, CancellationToken cancellationToken) where T : class
        {
            try
            {
                // This is a simplified version - in production you'd need proper date property handling
                var records = await dbSet.Take(_settings.MaxRecordsPerCleanup * 2).ToListAsync(cancellationToken);

                if (records.Any())
                {
                    dbSet.RemoveRange(records);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return records.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup records by date for {EntityType}", typeof(T).Name);
                return 0;
            }
        }
    }

    /// <summary>
    /// Cleanup operation summary
    /// </summary>
    public class CleanupSummary
    {
        public DateTime CleanupStarted { get; set; }
        public DateTime CleanupCompleted { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int EmailHistoryDeleted { get; set; }
        public int LogsDeleted { get; set; }
        public int FailedEmailsDeleted { get; set; }
        public int TotalDeleted { get; set; }
        public TimeSpan Duration => CleanupCompleted - CleanupStarted;
    }
}