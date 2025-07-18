namespace DT.EmailWorker.Core.Configuration
{
    /// <summary>
    /// Configuration settings for cleanup operations
    /// </summary>
    public class CleanupSettings
    {
        /// <summary>
        /// Number of days to retain data (default: 90 days)
        /// </summary>
        public int RetentionDays { get; set; } = 90;

        /// <summary>
        /// Maximum number of records to clean up in a single operation
        /// </summary>
        public int MaxRecordsPerCleanup { get; set; } = 1000;

        /// <summary>
        /// Whether automatic cleanup is enabled
        /// </summary>
        public bool EnableAutoCleanup { get; set; } = true;

        /// <summary>
        /// Path for storing backup files before cleanup
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// Email addresses to receive cleanup reports
        /// </summary>
        public List<string> CleanupReportRecipients { get; set; } = new List<string>();

        /// <summary>
        /// Whether to optimize database after cleanup
        /// </summary>
        public bool OptimizeDatabaseAfterCleanup { get; set; } = true;

        /// <summary>
        /// Retention days for email history records
        /// </summary>
        public int EmailHistoryRetentionDays { get; set; } = 180;

        /// <summary>
        /// Retention days for processing logs
        /// </summary>
        public int ProcessingLogsRetentionDays { get; set; } = 30;

        /// <summary>
        /// Retention days for service status records
        /// </summary>
        public int ServiceStatusRetentionDays { get; set; } = 7;

        /// <summary>
        /// Retention days for failed emails
        /// </summary>
        public int FailedEmailsRetentionDays { get; set; } = 30;

        /// <summary>
        /// Whether to create backups before major cleanup operations
        /// </summary>
        public bool CreateBackupBeforeCleanup { get; set; } = true;

        /// <summary>
        /// Retention days for backup files
        /// </summary>
        public int BackupRetentionDays { get; set; } = 30;

        /// <summary>
        /// Whether to send cleanup reports via email
        /// </summary>
        public bool SendCleanupReports { get; set; } = true;

        /// <summary>
        /// Time interval between cleanup operations (in hours)
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 24;

        /// <summary>
        /// Whether to perform aggressive cleanup (removes more data)
        /// </summary>
        public bool EnableAggressiveCleanup { get; set; } = false;

        /// <summary>
        /// Batch size for cleanup operations
        /// </summary>
        public int CleanupBatchSize { get; set; } = 500;
    }
}