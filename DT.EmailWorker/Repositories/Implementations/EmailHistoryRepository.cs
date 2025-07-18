using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT.EmailWorker.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for email history operations
    /// </summary>
    public class EmailHistoryRepository : IEmailHistoryRepository
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<EmailHistoryRepository> _logger;

        public EmailHistoryRepository(EmailDbContext context, ILogger<EmailHistoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailHistory> AddAsync(EmailHistory emailHistory, CancellationToken cancellationToken = default)
        {
            try
            {
                emailHistory.CreatedAt = DateTime.UtcNow;

                _context.EmailHistory.Add(emailHistory);
                await _context.SaveChangesAsync(cancellationToken);
                return emailHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add email history");
                throw;
            }
        }

        public async Task<EmailHistory?> GetByIdAsync(int historyId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email history by ID {HistoryId}", historyId);
                throw;
            }
        }

        // FIXED: Changed OriginalQueueId to QueueId
        public async Task<EmailHistory?> GetByQueueIdAsync(int queueId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .FirstOrDefaultAsync(h => h.QueueId == new Guid(queueId.ToString()), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email history by queue ID {QueueId}", queueId);
                throw;
            }
        }

        // Overload for Guid parameter (more appropriate)
        public async Task<EmailHistory?> GetByQueueIdAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .FirstOrDefaultAsync(h => h.QueueId == queueId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email history by queue ID {QueueId}", queueId);
                throw;
            }
        }

        public async Task<List<EmailHistory>> GetByRecipientAsync(string recipientEmail, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .Where(h => h.ToEmails.Contains(recipientEmail))
                    .OrderByDescending(h => h.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email history by recipient {RecipientEmail}", recipientEmail);
                throw;
            }
        }

        public async Task<List<EmailHistory>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .Where(h => h.SentAt >= fromDate && h.SentAt <= toDate)
                    .OrderByDescending(h => h.SentAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email history by date range");
                throw;
            }
        }

        public async Task<List<EmailHistory>> GetByStatusAsync(EmailQueueStatus status, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .Where(h => h.Status == status)
                    .OrderByDescending(h => h.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email history by status {Status}", status);
                throw;
            }
        }

        public async Task<List<EmailHistory>> GetByTemplateAsync(int templateId, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .Where(h => h.TemplateId == templateId)
                    .OrderByDescending(h => h.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email history by template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<List<EmailHistory>> GetFailedEmailsAsync(int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory
                    .Where(h => h.Status == EmailQueueStatus.Failed)
                    .OrderByDescending(h => h.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get failed emails");
                throw;
            }
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailHistory.CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total email history count");
                throw;
            }
        }

        public async Task<int> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            try
            {
                var recordsToDelete = await _context.EmailHistory
                    .Where(h => h.SentAt < olderThan)
                    .ToListAsync(cancellationToken);

                if (recordsToDelete.Any())
                {
                    _context.EmailHistory.RemoveRange(recordsToDelete);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return recordsToDelete.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old email history records");
                throw;
            }
        }

        public async Task<EmailDeliveryStatistics> GetDeliveryStatisticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var historyInRange = await _context.EmailHistory
                    .Where(h => h.SentAt >= fromDate && h.SentAt <= toDate)
                    .ToListAsync(cancellationToken);

                var totalProcessed = historyInRange.Count;
                var totalSent = historyInRange.Count(h => h.Status == EmailQueueStatus.Sent);
                var totalFailed = historyInRange.Count(h => h.Status == EmailQueueStatus.Failed);

                var statistics = new EmailDeliveryStatistics
                {
                    TotalProcessed = totalProcessed,
                    TotalSent = totalSent,
                    TotalFailed = totalFailed,
                    SuccessRate = totalProcessed > 0 ? (double)totalSent / totalProcessed * 100 : 0,
                    FromDate = fromDate,
                    ToDate = toDate,
                    // REMOVED: ByPriority grouping since EmailHistory doesn't have Priority property
                    // Note: Priority information would need to be tracked separately or joined from EmailQueue
                    ByTemplate = historyInRange
                        .Where(h => !string.IsNullOrEmpty(h.TemplateUsed)) // FIXED: Changed TemplateName to TemplateUsed
                        .GroupBy(h => h.TemplateUsed!)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get delivery statistics");
                throw;
            }
        }

        public async Task<List<EmailHistory>> SearchAsync(string searchTerm, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                var lowerSearchTerm = searchTerm.ToLower();

                return await _context.EmailHistory
                    .Where(h => h.Subject.ToLower().Contains(lowerSearchTerm) ||
                               h.ToEmails.ToLower().Contains(lowerSearchTerm) ||
                               h.TemplateUsed != null && h.TemplateUsed.ToLower().Contains(lowerSearchTerm)) // FIXED: Changed TemplateName to TemplateUsed
                    .OrderByDescending(h => h.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search email history with term {SearchTerm}", searchTerm);
                throw;
            }
        }
    }

    /// <summary>
    /// Email delivery statistics DTO
    /// </summary>
    public class EmailDeliveryStatistics
    {
        public int TotalProcessed { get; set; }
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public double SuccessRate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Dictionary<string, int> ByTemplate { get; set; } = new();
        // Note: Removed ByPriority since EmailHistory doesn't track priority
        // This would need to be implemented differently if priority statistics are needed
    }
}