using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
                log.CreatedAt = DateTime.UtcNow;

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

        public async Task<ProcessingLog> AddLogAsync(LogLevel level, string message, string? details = null,
            int? emailId = null, string? operationType = null, CancellationToken cancellationToken = default)
        {
            var log = new ProcessingLog
            {
                Level = level,
                Message = message,
                Details = details,
                EmailId = emailId,
                OperationType = operationType ?? "General",
                CreatedAt = DateTime.UtcNow
            };

            return await AddAsync(log, cancellationToken);
        }

        public async Task<List<ProcessingLog>> GetByEmailIdAsync(int emailId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ProcessingLogs
                    .Where(l => l.EmailId == emailId)
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
                    .Where(l => l.Level == level)
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
                    .Where(l => l.OperationType == operationType)
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
                var cutoffDate = DateTime.UtcNow.AddHours(-hoursBack);

                return await _context.ProcessingLogs
                    .Where(l => l.CreatedAt >= cutoffDate && l.Level == LogLevel.Error)
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
                var failedOperations = logs.Count(l => l.Level == LogLevel.Error);
                var successfulOperations = totalOperations - failedOperations;

                var metrics = new ProcessingMetrics
                {
                    TotalOperations = totalOperations,
                    SuccessfulOperations = successfulOperations,
                    FailedOperations = failedOperations,
                    SuccessRate = totalOperations > 0 ? (double)successfulOperations / totalOperations * 100 : 0,
                    FromDate = fromDate,
                    ToDate = toDate,
                    OperationCounts = logs
                        .GroupBy(l => l.OperationType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    LogLevelCounts = logs
                        .GroupBy(l => l.Level)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                // Calculate average processing time if available
                var processingTimeLogs = logs.Where(l => l.Details != null && l.Details.Contains("ms")).ToList();
                if (processingTimeLogs.Any())
                {
                    var processingTimes = new List<double>();
                    foreach (var log in processingTimeLogs)
                    {
                        if (TryExtractProcessingTime(log.Details!, out var time))
                        {
                            processingTimes.Add(time);
                        }
                    }

                    if (processingTimes.Any())
                    {
                        metrics.AverageProcessingTimeMs = processingTimes.Average();
                    }
                }

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
                var lowerSearchTerm = searchTerm.ToLower();

                return await _context.ProcessingLogs
                    .Where(l => l.Message.ToLower().Contains(lowerSearchTerm) ||
                               (l.Details != null && l.Details.ToLower().Contains(lowerSearchTerm)) ||
                               l.OperationType.ToLower().Contains(lowerSearchTerm))
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search processing logs with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        private static bool TryExtractProcessingTime(string details, out double timeMs)
        {
            timeMs = 0;
            try
            {
                // Look for patterns like "123.45ms" or "processed in 567ms"
                var match = System.Text.RegularExpressions.Regex.Match(details, @"(\d+\.?\d*)\s*ms");
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