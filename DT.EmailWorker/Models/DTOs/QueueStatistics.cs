namespace DT.EmailWorker.Models.DTOs
{
    /// <summary>
    /// Queue statistics data transfer object - matches interface requirements
    /// </summary>
    public class QueueStatistics
    {
        /// <summary>
        /// Number of emails pending processing
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Number of emails currently being processed
        /// </summary>
        public int ProcessingCount { get; set; }

        /// <summary>
        /// Number of successfully sent emails
        /// </summary>
        public int SentCount { get; set; }

        /// <summary>
        /// Number of failed emails
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Total number of emails in the queue
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// When statistics were last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Additional properties for compatibility
        /// <summary>
        /// Total number of queued emails (alias for PendingCount)
        /// </summary>
        public int TotalQueued
        {
            get => PendingCount;
            set => PendingCount = value;
        }

        /// <summary>
        /// Total number of emails currently processing (alias for ProcessingCount)
        /// </summary>
        public int TotalProcessing
        {
            get => ProcessingCount;
            set => ProcessingCount = value;
        }

        /// <summary>
        /// Total number of failed emails (alias for FailedCount)
        /// </summary>
        public int TotalFailed
        {
            get => FailedCount;
            set => FailedCount = value;
        }

        /// <summary>
        /// Total number of sent emails (alias for SentCount)
        /// </summary>
        public int TotalSent
        {
            get => SentCount;
            set => SentCount = value;
        }

        /// <summary>
        /// Number of high priority emails in queue
        /// </summary>
        public int HighPriorityCount { get; set; }

        /// <summary>
        /// Number of normal priority emails in queue
        /// </summary>
        public int NormalPriorityCount { get; set; }

        /// <summary>
        /// Number of low priority emails in queue
        /// </summary>
        public int LowPriorityCount { get; set; }

        /// <summary>
        /// Total number of scheduled emails
        /// </summary>
        public int TotalScheduled { get; set; }

        /// <summary>
        /// Timestamp of the oldest queued email
        /// </summary>
        public DateTime OldestQueuedEmail { get; set; }

        /// <summary>
        /// Average queue time in hours
        /// </summary>
        public double AverageQueueTimeHours { get; set; }

        /// <summary>
        /// Processing rate in emails per hour (calculated property)
        /// </summary>
        public double ProcessingRate
        {
            get
            {
                var timeSinceUpdate = DateTime.UtcNow - LastUpdated;
                return timeSinceUpdate.TotalHours > 0
                    ? ProcessingCount / timeSinceUpdate.TotalHours
                    : 0;
            }
        }

        /// <summary>
        /// Success rate as a percentage
        /// </summary>
        public double SuccessRate
        {
            get
            {
                var totalProcessed = SentCount + FailedCount;
                return totalProcessed > 0 ? (double)SentCount / totalProcessed * 100 : 0;
            }
        }

        /// <summary>
        /// Average processing time in milliseconds
        /// </summary>
        public int AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Age of oldest queued email in hours
        /// </summary>
        public int OldestQueuedEmailHours
        {
            get
            {
                if (OldestQueuedEmail == default(DateTime))
                    return 0;
                return (int)(DateTime.UtcNow - OldestQueuedEmail).TotalHours;
            }
        }
    }
}