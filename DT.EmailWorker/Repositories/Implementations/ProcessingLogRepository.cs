using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DT.EmailWorker.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for processing log operations
    /// </summary>
    public class ProcessingLogRepository : IProcessingLogRepository
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<ProcessingLogRepository> _logger;

        public ProcessingLogRepository(EmailDbContext context, ILogger<ProcessingLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ProcessingLog> AddAsync(ProcessingLog log, CancellationToken cancellationToken = default)
        {
            try
            {
                log.CreatedAt = DateTime.UtcNow.AddHours(3);

                _context.ProcessingLogs.Add(log);
                await _context.SaveChangesAsync(cancellationToken);
                return log;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add processing log");
                throw;
            }
        }

        // FIX: Updated method signature and property mapping to match ProcessingLog entity
        public async Task<ProcessingLog> AddLogAsync(LogLevel level, string message, string? details = null,
            int? emailId = null, string? operationType = null, CancellationToken cancellationToken = default)
        {
            var log = new ProcessingLog
            {
                LogLevel = level, // FIX: Use LogLevel instead of Level
                Category = operationType ?? "General", // FIX: Map operationType to Category
                Message = message,
                Exception = details, // FIX: Map details to Exception property
                ContextData = emailId?.ToString(), // FIX: Store emailId in ContextData as string
                ProcessingStep = operationType, // FIX: Also store in ProcessingStep
                CreatedAt = DateTime.UtcNow.AddHours(3)
            };

            return await AddAsync(log, cancellationToken);
        }

        // FIX: This method needs to be changed since ProcessingLog doesn't have EmailId
        // We'll search by ContextData containing the emailId
        public async Task<List<ProcessingLog>> GetByEmailIdAsync(int emailId, CancellationToken cancellationToken = default)
        {
            try
            {
                var emailIdString = emailId.ToString();
                return await _context.ProcessingLogs
                    .Where(l => l.ContextData != null && l.ContextData.Contains(emailIdString))
                    .OrderBy(l => l.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get processing logs by email ID {EmailId}", emailId);
                throw;
            }
        }

        public async Task<List<ProcessingLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ProcessingLogs
                    .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get processing logs by date range {FromDate} - {ToDate}", fromDate, toDate);
                throw;
            }
        }

        public async Task<List<ProcessingLog>> GetByLevelAsync(LogLevel level, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ProcessingLogs
                    .Where(l => l.LogLevel == level) // FIX: Use LogLevel instead of Level
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get processing logs by level {Level}", level);
                throw;
            }
        }

        public async Task<List<ProcessingLog>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ProcessingLogs
                    .Where(l => l.Category == operationType) // FIX: Use Category instead of OperationType
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get processing logs by operation type {OperationType}", operationType);
                throw;
            }
        }

        public async Task<int> DeleteOldLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            try
            {
                var logsToDelete = await _context.ProcessingLogs
                    .Where(l => l.CreatedAt < olderThan)
                    .ToListAsync(cancellationToken);

                if (logsToDelete.Any())
                {
                    _context.ProcessingLogs.RemoveRange(logsToDelete);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return logsToDelete.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old processing logs");
                throw;
            }
        }

        public async Task<List<ProcessingLog>> GetRecentErrorsAsync(int hoursBack = 24, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddHours(-hoursBack);

                return await _context.ProcessingLogs
                    .Where(l => l.CreatedAt >= cutoffDate && l.LogLevel == LogLevel.Error) // FIX: Use LogLevel
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent error logs");
                throw;
            }
        }

        public async Task<ProcessingMetrics> GetPerformanceMetricsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await _context.ProcessingLogs
                    .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                    .ToListAsync(cancellationToken);

                var totalOperations = logs.Count;
                var failedOperations = logs.Count(l => l.LogLevel == LogLevel.Error); // FIX: Use LogLevel
                var successfulOperations = totalOperations - failedOperations;

                var processingTimes = new List<double>();
                foreach (var log in logs)
                {
                    if (TryExtractProcessingTime(log.Message, out double timeMs))
                    {
                        processingTimes.Add(timeMs);
                    }
                }

                var metrics = new ProcessingMetrics
                {
                    TotalOperations = totalOperations,
                    SuccessfulOperations = successfulOperations,
                    FailedOperations = failedOperations,
                    SuccessRate = totalOperations > 0 ? (double)successfulOperations / totalOperations * 100 : 0,
                    AverageProcessingTimeMs = processingTimes.Any() ? processingTimes.Average() : 0,
                    OperationCounts = logs.GroupBy(l => l.Category) // FIX: Use Category instead of OperationType
                        .ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),
                    LogLevelCounts = logs.GroupBy(l => l.LogLevel) // FIX: Use LogLevel
                        .ToDictionary(g => g.Key, g => g.Count()),
                    FromDate = fromDate,
                    ToDate = toDate
                };

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get performance metrics");
                throw;
            }
        }

        public async Task<List<ProcessingLog>> SearchLogsAsync(string searchTerm, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ProcessingLogs
                    .Where(l => l.Message.Contains(searchTerm) ||
                               (l.Exception != null && l.Exception.Contains(searchTerm)) || // FIX: Use Exception instead of Details
                               (l.Category != null && l.Category.Contains(searchTerm))) // FIX: Use Category instead of OperationType
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search processing logs");
                throw;
            }
        }

        private bool TryExtractProcessingTime(string message, out double timeMs)
        {
            timeMs = 0;
            try
            {
                // Look for patterns like "123ms", "45.67ms", etc.
                var match = Regex.Match(message, @"(\d+(?:\.\d*)?)\s*ms");
                if (match.Success && double.TryParse(match.Groups[1].Value, out timeMs))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            return false;
        }
    }
}