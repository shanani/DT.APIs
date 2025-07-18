using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Service for managing email queue operations
    /// </summary>
    public interface IEmailQueueService
    {
        /// <summary>
        /// Get pending emails from the queue for processing
        /// </summary>
        /// <param name="batchSize">Number of emails to retrieve</param>
        /// <param name="workerId">ID of the worker requesting emails</param>
        /// <returns>List of emails ready for processing</returns>
        Task<List<EmailProcessingRequest>> GetPendingEmailsAsync(int batchSize, string workerId);

        /// <summary>
        /// Get scheduled emails that are ready to be sent
        /// </summary>
        /// <param name="batchSize">Number of emails to retrieve</param>
        /// <returns>List of scheduled emails ready for processing</returns>
        Task<List<EmailProcessingRequest>> GetDueScheduledEmailsAsync(int batchSize);

        /// <summary>
        /// Mark an email as being processed
        /// </summary>
        /// <param name="queueId">Queue ID of the email</param>
        /// <param name="workerId">ID of the worker processing the email</param>
        /// <returns>Task</returns>
        Task MarkAsProcessingAsync(Guid queueId, string workerId);

        /// <summary>
        /// Mark an email as successfully sent
        /// </summary>
        /// <param name="queueId">Queue ID of the email</param>
        /// <param name="workerId">ID of the worker that processed the email</param>
        /// <param name="processingTimeMs">Processing time in milliseconds</param>
        /// <returns>Task</returns>
        Task MarkAsSentAsync(Guid queueId, string workerId, int processingTimeMs);

        /// <summary>
        /// Mark an email as failed
        /// </summary>
        /// <param name="queueId">Queue ID of the email</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="shouldRetry">Whether the email should be retried</param>
        /// <returns>Task</returns>
        Task MarkAsFailedAsync(Guid queueId, string errorMessage, bool shouldRetry = true);

        /// <summary>
        /// Get emails that need to be retried
        /// </summary>
        /// <param name="batchSize">Number of emails to retrieve</param>
        /// <param name="maxRetryCount">Maximum retry count to consider</param>
        /// <returns>List of emails to retry</returns>
        Task<List<EmailProcessingRequest>> GetEmailsForRetryAsync(int batchSize, int maxRetryCount);

        /// <summary>
        /// Queue a new email for processing
        /// </summary>
        /// <param name="emailRequest">Email processing request</param>
        /// <returns>Queue ID of the created email</returns>
        Task<Guid> QueueEmailAsync(EmailProcessingRequest emailRequest);

        /// <summary>
        /// Queue multiple emails for processing
        /// </summary>
        /// <param name="emailRequests">List of email processing requests</param>
        /// <returns>List of queue IDs</returns>
        Task<List<Guid>> QueueBulkEmailsAsync(List<EmailProcessingRequest> emailRequests);

        /// <summary>
        /// Cancel a queued email
        /// </summary>
        /// <param name="queueId">Queue ID of the email to cancel</param>
        /// <returns>True if cancelled successfully</returns>
        Task<bool> CancelEmailAsync(Guid queueId);

        /// <summary>
        /// Get queue statistics
        /// </summary>
        /// <returns>Queue statistics</returns>
        Task<QueueStatistics> GetQueueStatisticsAsync();

        /// <summary>
        /// Get emails stuck in processing state
        /// </summary>
        /// <param name="stuckThresholdMinutes">Minutes after which an email is considered stuck</param>
        /// <returns>List of stuck emails</returns>
        Task<List<EmailQueue>> GetStuckEmailsAsync(int stuckThresholdMinutes);

        /// <summary>
        /// Reset stuck emails to queued status
        /// </summary>
        /// <param name="stuckThresholdMinutes">Minutes after which an email is considered stuck</param>
        /// <returns>Number of emails reset</returns>
        Task<int> ResetStuckEmailsAsync(int stuckThresholdMinutes);

        /// <summary>
        /// Get email details by queue ID
        /// </summary>
        /// <param name="queueId">Queue ID</param>
        /// <returns>Email queue entity or null if not found</returns>
        Task<EmailQueue?> GetEmailByQueueIdAsync(Guid queueId);

        /// <summary>
        /// Update email priority
        /// </summary>
        /// <param name="queueId">Queue ID</param>
        /// <param name="priority">New priority</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateEmailPriorityAsync(Guid queueId, EmailPriority priority);

        /// <summary>
        /// Schedule an email for later delivery
        /// </summary>
        /// <param name="queueId">Queue ID</param>
        /// <param name="scheduledFor">When to send the email</param>
        /// <returns>True if scheduled successfully</returns>
        Task<bool> ScheduleEmailAsync(Guid queueId, DateTime scheduledFor);
    }

    /// <summary>
    /// Queue statistics DTO
    /// </summary>
    public class QueueStatistics
    {
        public int TotalQueued { get; set; }
        public int TotalProcessing { get; set; }
        public int TotalFailed { get; set; }
        public int TotalScheduled { get; set; }
        public int HighPriorityCount { get; set; }
        public int NormalPriorityCount { get; set; }
        public int LowPriorityCount { get; set; }
        public DateTime OldestQueuedEmail { get; set; }
        public double AverageQueueTimeHours { get; set; }
    }
}