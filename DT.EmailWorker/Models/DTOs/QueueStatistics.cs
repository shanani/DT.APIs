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
        public int TotalQueued { get; internal set; }
        public int TotalProcessing { get; internal set; }
        public int TotalFailed { get; internal set; }
        public int HighPriorityCount { get; internal set; }
        public int NormalPriorityCount { get; internal set; }
        public int LowPriorityCount { get; internal set; }
        public int TotalScheduled { get; internal set; }
        public DateTime OldestQueuedEmail { get; internal set; }
        public double AverageQueueTimeHours { get; internal set; }
    }
}