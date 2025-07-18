using DT.EmailWorker.Models.Entities;
using Microsoft.Extensions.Logging;

namespace DT.EmailWorker.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for processing log operations
    /// </summary>
    public interface IProcessingLogRepository
    {
        /// <summary>
        /// Add processing log entry
        /// </summary>
        /// <param name="log">Processing log entry</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Added log entry with ID</returns>
        Task<ProcessingLog> AddAsync(ProcessingLog log, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add processing log entry with convenience method
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Log message</param>
        /// <param name="details">Additional details</param>
        /// <param name="emailId">Related email ID</param>
        /// <param name="operationType">Operation type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Added log entry with ID</returns>
        Task<ProcessingLog> AddLogAsync(LogLevel level, string message, string? details = null,
            int? emailId = null, string? operationType = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get processing logs by email ID
        /// </summary>
        /// <param name="emailId">Email ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of processing logs for the email</returns>
        Task<List<ProcessingLog>> GetByEmailIdAsync(int emailId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get processing logs by date range
        /// </summary>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of processing logs within date range</returns>
        Task<List<ProcessingLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get processing logs by level
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of processing logs</returns>
        Task<List<ProcessingLog>> GetByLevelAsync(LogLevel level, int pageSize, int pageNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get processing logs by operation type
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of processing logs for the operation type</returns>
        Task<List<ProcessingLog>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete old processing log entries
        /// </summary>
        /// <param name="olderThan">Delete entries older than this date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted entries</returns>
        Task<int> DeleteOldLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get error logs for troubleshooting
        /// </summary>
        /// <param name="hoursBack">Hours to look back</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of error logs</returns>
        Task<List<ProcessingLog>> GetRecentErrorsAsync(int hoursBack = 24, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get processing performance metrics
        /// </summary>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance metrics</returns>
        Task<ProcessingMetrics> GetPerformanceMetricsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search processing logs
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        Task<List<ProcessingLog>> SearchLogsAsync(string searchTerm, int pageSize, int pageNumber, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Processing performance metrics DTO
    /// </summary>
    public class ProcessingMetrics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double SuccessRate { get; set; }
        public double AverageProcessingTimeMs { get; set; }
        public Dictionary<string, int> OperationCounts { get; set; } = new();
        public Dictionary<LogLevel, int> LogLevelCounts { get; set; } = new();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}