namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Service for cleanup and archival operations
    /// </summary>
    public interface ICleanupService
    {

        Task<int> CleanupProcessingLogsAsync(int retentionDays, CancellationToken cancellationToken = default);

        Task<int> CleanupEmailAttachmentsAsync(int retentionDays, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clean up old email history records
        /// </summary>
        /// <param name="retentionDays">Number of days to retain</param>
        /// <returns>Number of records cleaned up</returns>
        Task<int> CleanupEmailHistoryAsync(int retentionDays, CancellationToken cancellationToken = default);


        /// <summary>
        /// Clean up old service status records
        /// </summary>
        /// <param name="retentionDays">Number of days to retain</param>
        /// <returns>Number of records cleaned up</returns>
        Task<int> CleanupServiceStatusAsync(int retentionDays);

        /// <summary>
        /// Archive old email history to files
        /// </summary>
        /// <param name="retentionDays">Number of days after which to archive</param>
        /// <param name="archivePath">Path to store archive files</param>
        /// <returns>Archive operation result</returns>
        Task<ArchiveResult> ArchiveEmailHistoryAsync(int retentionDays, string? archivePath = null);

        /// <summary>
        /// Clean up failed emails that are beyond retry attempts
        /// </summary>
        /// <param name="retentionDays">Number of days to retain failed emails</param>
        /// <returns>Number of failed emails cleaned up</returns>
        Task<int> CleanupFailedEmailsAsync(int retentionDays);

        /// <summary>
        /// Clean up orphaned attachments that don't have corresponding emails
        /// </summary>
        /// <returns>Number of orphaned attachments cleaned up</returns>
        Task<int> CleanupOrphanedAttachmentsAsync();

        /// <summary>
        /// Perform comprehensive cleanup operation
        /// </summary>
        /// <returns>Cleanup summary result</returns>
        Task<CleanupSummary> PerformFullCleanupAsync();

        /// <summary>
        /// Optimize database after cleanup operations
        /// </summary>
        /// <returns>True if optimization completed successfully</returns>
        Task<bool> OptimizeDatabaseAsync();

        /// <summary>
        /// Get cleanup statistics
        /// </summary>
        /// <returns>Current cleanup statistics</returns>
        Task<CleanupStatistics> GetCleanupStatisticsAsync();

        /// <summary>
        /// Estimate cleanup impact before performing actual cleanup
        /// </summary>
        /// <param name="retentionDays">Number of days to retain</param>
        /// <returns>Estimated cleanup impact</returns>
        Task<CleanupEstimate> EstimateCleanupImpactAsync(int retentionDays);

        /// <summary>
        /// Create backup before major cleanup operations
        /// </summary>
        /// <param name="backupPath">Path to store backup files</param>
        /// <returns>Backup operation result</returns>
        Task<BackupResult> CreateBackupAsync(string? backupPath = null);

        /// <summary>
        /// Clean up old backup files
        /// </summary>
        /// <param name="retentionDays">Number of days to retain backups</param>
        /// <returns>Number of backup files cleaned up</returns>
        Task<int> CleanupOldBackupsAsync(int retentionDays);

        /// <summary>
        /// Check disk space and recommend cleanup actions
        /// </summary>
        /// <returns>Disk space analysis result</returns>
        Task<DiskSpaceAnalysis> AnalyzeDiskSpaceAsync();

        /// <summary>
        /// Perform aggressive cleanup when disk space is low
        /// </summary>
        /// <param name="targetFreeSpacePercent">Target free space percentage</param>
        /// <returns>Aggressive cleanup result</returns>
        Task<CleanupSummary> PerformAggressiveCleanupAsync(int targetFreeSpacePercent);
    }

    /// <summary>
    /// Archive operation result
    /// </summary>
    public class ArchiveResult
    {
        public bool Success { get; set; }
        public int RecordsArchived { get; set; }
        public string ArchiveFilePath { get; set; } = string.Empty;
        public long ArchiveFileSizeBytes { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ArchiveFiles { get; set; } = new List<string>();
    }

    /// <summary>
    /// Cleanup operation summary
    /// </summary>
    public class CleanupSummary
    {
        public int EmailHistoryRecordsCleaned { get; set; }
        public int ProcessingLogsCleaned { get; set; }
        public int AttachmentsCleaned { get; set; }
        public int ServiceStatusRecordsCleaned { get; set; }
        public int FailedEmailsCleaned { get; set; }
        public int OrphanedAttachmentsCleaned { get; set; }
        public long SpaceFreedBytes { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public DateTime CleanupStarted { get; set; }
        public DateTime CleanupCompleted { get; set; }
        public bool DatabaseOptimized { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Current cleanup statistics
    /// </summary>
    public class CleanupStatistics
    {
        public int TotalEmailHistoryRecords { get; set; }
        public int TotalProcessingLogs { get; set; }
        public int TotalAttachments { get; set; }
        public int TotalServiceStatusRecords { get; set; }
        public int RecordsOlderThan30Days { get; set; }
        public int RecordsOlderThan90Days { get; set; }
        public int RecordsOlderThan180Days { get; set; }
        public long TotalDatabaseSizeBytes { get; set; }
        public long EstimatedCleanableBytes { get; set; }
        public DateTime OldestRecord { get; set; }
        public DateTime LastCleanupRun { get; set; }
    }

    /// <summary>
    /// Cleanup impact estimation
    /// </summary>
    public class CleanupEstimate
    {
        public int EmailHistoryRecordsToClean { get; set; }
        public int ProcessingLogsToClean { get; set; }
        public int AttachmentsToClean { get; set; }
        public long EstimatedSpaceToFreeBytes { get; set; }
        public TimeSpan EstimatedProcessingTime { get; set; }
        public double EstimatedDatabaseSizeReductionPercent { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Backup operation result
    /// </summary>
    public class BackupResult
    {
        public bool Success { get; set; }
        public string BackupFilePath { get; set; } = string.Empty;
        public long BackupFileSizeBytes { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime BackupCreated { get; set; }
        public string BackupType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Disk space analysis result
    /// </summary>
    public class DiskSpaceAnalysis
    {
        public long TotalDiskSpaceBytes { get; set; }
        public long FreeDiskSpaceBytes { get; set; }
        public long UsedDiskSpaceBytes { get; set; }
        public double FreeSpacePercent { get; set; }
        public double UsedSpacePercent { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public double DatabaseSpacePercent { get; set; }
        public bool IsLowOnSpace { get; set; }
        public bool RequiresCleanup { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public long EstimatedReclaimableBytes { get; set; }
    }
}