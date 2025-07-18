using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;

namespace DT.EmailWorker.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for email queue operations
    /// </summary>
    public interface IEmailQueueRepository
    {
        /// <summary>
        /// Get pending emails for processing
        /// </summary>
        /// <param name="batchSize">Maximum number of emails to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of pending emails</returns>
        Task<List<EmailQueue>> GetPendingEmailsAsync(int batchSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get emails by priority
        /// </summary>
        /// <param name="priority">Email priority</param>
        /// <param name="batchSize">Maximum number of emails to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of emails with specified priority</returns>
        Task<List<EmailQueue>> GetEmailsByPriorityAsync(EmailPriority priority, int batchSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get emails that failed processing and are eligible for retry
        /// </summary>
        /// <param name="maxRetries">Maximum retry count</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of emails eligible for retry</returns>
        Task<List<EmailQueue>> GetFailedEmailsForRetryAsync(int maxRetries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update email status
        /// </summary>
        /// <param name="emailId">Email ID</param>
        /// <param name="status">New status</param>
        /// <param name="errorMessage">Error message if failed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateEmailStatusAsync(int emailId, EmailQueueStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increment retry count for an email
        /// </summary>
        /// <param name="emailId">Email ID</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task IncrementRetryCountAsync(int emailId, string errorMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email by ID
        /// </summary>
        /// <param name="emailId">Email ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email queue item or null</returns>
        Task<EmailQueue?> GetByIdAsync(int emailId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new email to queue
        /// </summary>
        /// <param name="email">Email to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Added email with ID</returns>
        Task<EmailQueue> AddAsync(EmailQueue email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get queue statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Queue statistics</returns>
        Task<QueueStatistics> GetQueueStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete processed emails older than specified date
        /// </summary>
        /// <param name="olderThan">Delete emails older than this date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted emails</returns>
        Task<int> DeleteOldProcessedEmailsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get emails by status
        /// </summary>
        /// <param name="status">Email status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of emails with specified status</returns>
        Task<List<EmailQueue>> GetEmailsByStatusAsync(EmailQueueStatus status, CancellationToken cancellationToken = default);
    }

     
    
}