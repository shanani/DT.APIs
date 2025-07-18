using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;

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
        /// Get email history by original queue ID
        /// </summary>
        /// <param name="queueId">Original queue ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email history or null</returns>
        Task<EmailHistory?> GetByQueueIdAsync(int queueId, CancellationToken cancellationToken = default);

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
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of email history with specified status</returns>
        Task<List<EmailHistory>> GetByStatusAsync(EmailQueueStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete old email history records
        /// </summary>
        /// <param name="olderThan">Delete records older than this date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted records</returns>
        Task<int> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get email delivery statistics
        /// </summary>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delivery statistics</returns>
        Task<EmailDeliveryStatistics> GetDeliveryStatisticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search email history
        /// </summary>
        /// <param name="searchTerm">Search term (subject, recipient, etc.)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        Task<List<EmailHistory>> SearchAsync(string searchTerm, int pageSize, int pageNumber, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Email delivery statistics DTO
    /// </summary>
    public class EmailDeliveryStatistics
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public int TotalProcessed { get; set; }
        public double SuccessRate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Dictionary<EmailPriority, int> ByPriority { get; set; } = new();
        public Dictionary<string, int> ByTemplate { get; set; } = new();
    }
}