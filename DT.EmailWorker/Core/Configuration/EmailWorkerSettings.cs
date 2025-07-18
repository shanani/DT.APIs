namespace DT.EmailWorker.Core.Configuration
{
    /// <summary>
    /// Main configuration settings for the Email Worker service
    /// </summary>
    public class EmailWorkerSettings
    {
        /// <summary>
        /// Service name for identification
        /// </summary>
        public string ServiceName { get; set; } = "DT.EmailWorker";

        /// <summary>
        /// Maximum number of concurrent workers for email processing
        /// </summary>
        public int MaxConcurrentWorkers { get; set; } = 5;

        /// <summary>
        /// Number of emails to process in each batch
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// Interval in seconds between queue processing cycles
        /// </summary>
        public int ProcessingIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts for failed emails
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay in minutes between retry attempts
        /// </summary>
        public int RetryDelayMinutes { get; set; } = 5;

        /// <summary>
        /// Interval in minutes between health checks
        /// </summary>
        public int HealthCheckIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Interval in hours between cleanup operations
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 24;

        /// <summary>
        /// Number of days to retain email history
        /// </summary>
        public int EmailHistoryRetentionDays { get; set; } = 180;

        /// <summary>
        /// Number of days to retain processing logs
        /// </summary>
        public int LogRetentionDays { get; set; } = 30;

        /// <summary>
        /// Whether scheduled emails are enabled
        /// </summary>
        public bool EnableScheduledEmails { get; set; } = true;

        /// <summary>
        /// Whether automatic cleanup is enabled
        /// </summary>
        public bool EnableAutoCleanup { get; set; } = true;

        /// <summary>
        /// Whether template processing is enabled
        /// </summary>
        public bool EnableTemplateProcessing { get; set; } = true;

        /// <summary>
        /// Whether email validation is enabled
        /// </summary>
        public bool EnableEmailValidation { get; set; } = true;

        /// <summary>
        /// Whether health monitoring is enabled
        /// </summary>
        public bool EnableHealthMonitoring { get; set; } = true;

        /// <summary>
        /// Whether performance metrics collection is enabled
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Connection string name for the email database
        /// </summary>
        public string DatabaseConnectionName { get; set; } = "EmailDbConn";

        /// <summary>
        /// Timeout in seconds for database operations
        /// </summary>
        public int DatabaseTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum size in MB for email attachments
        /// </summary>
        public int MaxAttachmentSizeMB { get; set; } = 25;

        /// <summary>
        /// Maximum size in MB for total email size
        /// </summary>
        public int MaxEmailSizeMB { get; set; } = 50;

        /// <summary>
        /// Maximum number of recipients per email
        /// </summary>
        public int MaxRecipientsPerEmail { get; set; } = 100;

        /// <summary>
        /// Worker identification prefix
        /// </summary>
        public string WorkerIdPrefix { get; set; } = "EMAILWKR";

        /// <summary>
        /// Enable detailed debug logging
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>
        /// Enable performance counters
        /// </summary>
        public bool EnablePerformanceCounters { get; set; } = true;

        /// <summary>
        /// Thread sleep time in milliseconds when queue is empty
        /// </summary>
        public int EmptyQueueSleepMs { get; set; } = 10000;

        /// <summary>
        /// Maximum processing time in minutes before considering email stuck
        /// </summary>
        public int MaxProcessingTimeMinutes { get; set; } = 10;
    }
}