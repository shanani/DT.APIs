using DT.EmailWorker.Models.Entities;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Service for managing scheduled email operations
    /// </summary>
    public interface ISchedulingService
    {
        /// <summary>
        /// Schedule an email to be sent at a specific time
        /// </summary>
        /// <param name="scheduledEmail">Scheduled email details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Scheduled email with ID</returns>
        Task<ScheduledEmail> ScheduleEmailAsync(ScheduledEmail scheduledEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get emails that are due to be sent
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of emails due for sending</returns>
        Task<List<ScheduledEmail>> GetDueEmailsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel a scheduled email
        /// </summary>
        /// <param name="scheduledEmailId">Scheduled email ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully cancelled</returns>
        Task<bool> CancelScheduledEmailAsync(int scheduledEmailId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update scheduled email send time
        /// </summary>
        /// <param name="scheduledEmailId">Scheduled email ID</param>
        /// <param name="newSendTime">New send time</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully updated</returns>
        Task<bool> RescheduleEmailAsync(int scheduledEmailId, DateTime newSendTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get scheduled emails by date range
        /// </summary>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of scheduled emails</returns>
        Task<List<ScheduledEmail>> GetScheduledEmailsByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process due emails and add them to the regular queue
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of emails processed</returns>
        Task<int> ProcessDueEmailsAsync(CancellationToken cancellationToken = default);
    }
}