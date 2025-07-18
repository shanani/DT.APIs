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
    }
}