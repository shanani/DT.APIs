namespace DT.EmailWorker.Models.DTOs
{
    /// <summary>
    /// Queue statistics data transfer object
    /// </summary>
    public class QueueStatistics
    {
        /// <summary>
        /// Total number of emails in the queue
        /// </summary>
        public int TotalCount { get; set; }

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
        /// Number of cancelled emails
        /// </summary>
        public int CancelledCount { get; set; }

        /// <summary>
        /// Number of scheduled emails
        /// </summary>
        public int ScheduledCount { get; set; }

        /// <summary>
        /// Average processing time in milliseconds
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Oldest pending email timestamp
        /// </summary>
        public DateTime? OldestPendingEmail { get; set; }

        /// <summary>
        /// Total queued emails (pending + processing + scheduled)
        /// </summary>
        public int TotalQueued => PendingCount + ProcessingCount + ScheduledCount;

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate => TotalCount > 0 ? (double)SentCount / TotalCount * 100 : 0;

        /// <summary>
        /// Failure rate percentage
        /// </summary>
        public double FailureRate => TotalCount > 0 ? (double)FailedCount / TotalCount * 100 : 0;

        /// <summary>
        /// Number of emails processed in the last hour
        /// </summary>
        public int ProcessedLastHour { get; set; }

        /// <summary>
        /// Number of emails failed in the last hour
        /// </summary>
        public int FailedLastHour { get; set; }

        /// <summary>
        /// Statistics timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// High priority emails count
        /// </summary>
        public int HighPriorityCount { get; set; }

        /// <summary>
        /// Normal priority emails count
        /// </summary>
        public int NormalPriorityCount { get; set; }

        /// <summary>
        /// Low priority emails count
        /// </summary>
        public int LowPriorityCount { get; set; }
    }
}