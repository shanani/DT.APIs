namespace DT.EmailWorker.Core.Configuration
{
    /// <summary>
    /// Cleanup and archival configuration settings
    /// </summary>
    public class CleanupSettings
    {
        /// <summary>
        /// Whether automatic cleanup is enabled
        /// </summary>
        public bool EnableAutoCleanup { get; set; } = true;

        /// <summary>
        /// Number of days to retain email history
        /// </summary>
        public int EmailHistoryRetentionDays { get; set; } = 180;

        /// <summary>
        /// Number of days to retain processing logs
        /// </summary>
        public int ProcessingLogRetentionDays { get; set; } = 30;

        /// <summary>
        /// Number of days to retain failed emails for debugging
        /// </summary>
        public int FailedEmailRetentionDays { get; set; } = 7;

        /// <summary>
        /// Number of days to retain successful emails
        /// </summary>
        public int SuccessfulEmailRetentionDays { get; set; } = 30;

        /// <summary>
        /// Whether to archive old emails instead of deleting them
        /// </summary>
        public bool ArchiveOldEmails { get; set; } = true;

        /// <summary>
        /// Archive file path (null = use default temp location)
        /// </summary>
        public string? ArchivePath { get; set; }

        /// <summary>
        /// Whether to compress archived emails
        /// </summary>
        public bool CompressArchives { get; set; } = true;

        /// <summary>
        /// Archive file format (zip, 7z, tar.gz)
        /// </summary>
        public string ArchiveFormat { get; set; } = "zip";

        /// <summary>
        /// Maximum archive file size in MB before creating a new archive
        /// </summary>
        public int MaxArchiveFileSizeMB { get; set; } = 100;

        /// <summary>
        /// Interval in hours between cleanup operations
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 24;

        /// <summary>
        /// Time of day to run cleanup (24-hour format, e.g., "02:00")
        /// </summary>
        public string CleanupTime { get; set; } = "02:00";

        /// <summary>
        /// Number of records to process in each cleanup batch
        /// </summary>
        public int CleanupBatchSize { get; set; } = 1000;

        /// <summary>
        /// Whether to clean up attachments with orphaned emails
        /// </summary>
        public bool CleanupOrphanedAttachments { get; set; } = true;

        /// <summary>
        /// Whether to clean up old service status records
        /// </summary>
        public bool CleanupOldServiceStatus { get; set; } = true;

        /// <summary>
        /// Number of days to retain service status history
        /// </summary>
        public int ServiceStatusRetentionDays { get; set; } = 7;

        /// <summary>
        /// Whether to vacuum/optimize database after cleanup
        /// </summary>
        public bool OptimizeDatabaseAfterCleanup { get; set; } = true;

        /// <summary>
        /// Whether to send cleanup reports
        /// </summary>
        public bool SendCleanupReports { get; set; } = false;

        /// <summary>
        /// Email addresses to send cleanup reports to
        /// </summary>
        public List<string> CleanupReportRecipients { get; set; } = new List<string>();

        /// <summary>
        /// Whether to backup before major cleanup operations
        /// </summary>
        public bool BackupBeforeCleanup { get; set; } = false;

        /// <summary>
        /// Backup retention period in days
        /// </summary>
        public int BackupRetentionDays { get; set; } = 30;

        /// <summary>
        /// Whether to enable aggressive cleanup during high storage usage
        /// </summary>
        public bool EnableAggressiveCleanup { get; set; } = false;

        /// <summary>
        /// Disk usage percentage threshold to trigger aggressive cleanup
        /// </summary>
        public int AggressiveCleanupThresholdPercent { get; set; } = 85;
    }
}