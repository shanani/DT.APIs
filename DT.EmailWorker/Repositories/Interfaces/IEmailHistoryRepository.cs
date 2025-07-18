using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Repositories.Implementations;

namespace DT.EmailWorker.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for email history operations
    /// </summary>
    public interface IEmailHistoryRepository
    {
        /// <summary>
        /// Add email to history
        /// </summary>
        /// <param name="emailHistory">Email history record</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Added email history with ID</returns>
        Task<EmailHistory> AddAsync(EmailHistory emailHistory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email history by ID
        /// </summary>
        /// <param name="historyId">History ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email history or null</returns>
        Task<EmailHistory?> GetByIdAsync(int historyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email history by original queue ID (int overload for backward compatibility)
        /// </summary>
        /// <param name="queueId">Original queue ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email history or null</returns>
        Task<EmailHistory?> GetByQueueIdAsync(int queueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email history by original queue ID (Guid - preferred method)
        /// </summary>
        /// <param name="queueId">Original queue ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email history or null</returns>
        Task<EmailHistory?> GetByQueueIdAsync(Guid queueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email history by recipient
        /// </summary>
        /// <param name="recipientEmail">Recipient email address</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of email history</returns>
        Task<List<EmailHistory>> GetByRecipientAsync(string recipientEmail, int pageSize, int pageNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email history by date range
        /// </summary>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of email history within date range</returns>
        Task<List<EmailHistory>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email history by status
        /// </summary>
        /// <param name="status">Email status</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of email history</returns>
        Task<List<EmailHistory>> GetByStatusAsync(EmailQueueStatus status, int pageSize, int pageNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email history by template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of email history</returns>
        Task<List<EmailHistory>> GetByTemplateAsync(int templateId, int pageSize, int pageNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get failed emails
        /// </summary>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of failed emails</returns>
        Task<List<EmailHistory>> GetFailedEmailsAsync(int pageSize, int pageNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get total count of email history records
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total count</returns>
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete old email history records
        /// </summary>
        /// <param name="olderThan">Delete records older than this date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted records</returns>
        Task<int> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get delivery statistics for a date range
        /// </summary>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delivery statistics</returns>
        Task<EmailDeliveryStatistics> GetDeliveryStatisticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search email history
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        Task<List<EmailHistory>> SearchAsync(string searchTerm, int pageSize, int pageNumber, CancellationToken cancellationToken = default);
    }
}