namespace DT.EmailWorker.Models.Enums
{
    /// <summary>
    /// Status of email in the processing queue
    /// </summary>
    public enum EmailQueueStatus
    {
        /// <summary>
        /// Email is queued and waiting to be processed
        /// </summary>
        Queued = 0,

        /// <summary>
        /// Email is currently being processed
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Email has been sent successfully
        /// </summary>
        Sent = 2,

        /// <summary>
        /// Email processing failed
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Email was cancelled before processing
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// Email is scheduled for future delivery
        /// </summary>
        Scheduled = 5,
        Pending = 6
    }
}