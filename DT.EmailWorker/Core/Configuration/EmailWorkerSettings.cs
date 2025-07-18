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
        /// Email queue polling interval in seconds
        /// </summary>
        public int PollingIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Scheduled email check interval in minutes
        /// </summary>
        public int ScheduledEmailCheckIntervalMinutes { get; set; } = 1;

        /// <summary>
        /// Email address for status reports
        /// </summary>
        public string? StatusReportEmail { get; set; }

        /// <summary>
        /// Email address for alerts
        /// </summary>
        public string? AlertEmail { get; set; }

        /// <summary>
        /// Webhook URL for notifications
        /// </summary>
        public string? WebhookUrl { get; set; }

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Service description
        /// </summary>
        public string Description { get; set; } = "Enterprise Email Processing Service";
    }
}