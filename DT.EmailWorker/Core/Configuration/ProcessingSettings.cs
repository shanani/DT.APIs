namespace DT.EmailWorker.Core.Configuration
{
    /// <summary>
    /// Email processing configuration settings
    /// </summary>
    public class ProcessingSettings
    {
        /// <summary>
        /// Batch size for processing emails
        /// </summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>
        /// Maximum number of concurrent workers
        /// </summary>
        public int MaxConcurrentWorkers { get; set; } = 5;

        /// <summary>
        /// Maximum retry attempts for failed emails
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in minutes
        /// </summary>
        public int RetryDelayMinutes { get; set; } = 15;

        /// <summary>
        /// Maximum processing time per email in minutes
        /// </summary>
        public int MaxProcessingTimeMinutes { get; set; } = 10;

        /// <summary>
        /// Processing timeout in minutes
        /// </summary>
        public int ProcessingTimeoutMinutes { get; set; } = 10;

        /// <summary>
        /// Maximum attachment size in MB
        /// </summary>
        public int MaxAttachmentSizeMB { get; set; } = 25;

        /// <summary>
        /// Maximum total email size in MB
        /// </summary>
        public int MaxEmailSizeMB { get; set; } = 50;

        /// <summary>
        /// Maximum number of recipients per email
        /// </summary>
        public int MaxRecipientsPerEmail { get; set; } = 100;

        /// <summary>
        /// Whether template processing is enabled
        /// </summary>
        public bool EnableTemplateProcessing { get; set; } = true;

        /// <summary>
        /// Whether email validation is enabled
        /// </summary>
        public bool EnableEmailValidation { get; set; } = true;

        /// <summary>
        /// Whether HTML optimization is enabled
        /// </summary>
        public bool EnableHtmlOptimization { get; set; } = true;

        /// <summary>
        /// Whether mobile optimization is enabled
        /// </summary>
        public bool EnableMobileOptimization { get; set; } = true;

        /// <summary>
        /// Whether CID image processing is enabled
        /// </summary>
        public bool EnableCidImageProcessing { get; set; } = true;

        /// <summary>
        /// Whether attachment processing is enabled
        /// </summary>
        public bool EnableAttachmentProcessing { get; set; } = true;

        /// <summary>
        /// Maximum number of placeholders allowed in templates
        /// </summary>
        public int MaxPlaceholders { get; set; } = 100;

        /// <summary>
        /// Placeholder pattern for template processing (regex)
        /// </summary>
        public string PlaceholderPattern { get; set; } = @"\{([^}]+)\}";

        /// <summary>
        /// Whether to validate placeholders during template processing
        /// </summary>
        public bool ValidatePlaceholders { get; set; } = true;

        /// <summary>
        /// Whether to log missing placeholders
        /// </summary>
        public bool LogMissingPlaceholders { get; set; } = true;

        /// <summary>
        /// Whether to enable parallel attachment processing
        /// </summary>
        public bool EnableParallelAttachmentProcessing { get; set; } = true;

        /// <summary>
        /// Maximum number of concurrent attachment processes
        /// </summary>
        public int MaxConcurrentAttachmentProcesses { get; set; } = 3;

        /// <summary>
        /// Whether to enable batch processing
        /// </summary>
        public bool EnableBatchProcessing { get; set; } = true;

        /// <summary>
        /// Minimum batch size for parallel processing
        /// </summary>
        public int MinBatchSizeForParallel { get; set; } = 10;

        /// <summary>
        /// Whether to enable performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Performance monitoring interval in seconds
        /// </summary>
        public int PerformanceMonitoringIntervalSeconds { get; set; } = 60;
    }
}