using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT.EmailWorker.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for email queue operations
    /// </summary>
    public class EmailQueueRepository : IEmailQueueRepository
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<EmailQueueRepository> _logger;

        public EmailQueueRepository(EmailDbContext context, ILogger<EmailQueueRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<EmailQueue>> GetPendingEmailsAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailQueue
                    .Include(e => e.Attachments)
                    .Where(e => e.Status == EmailQueueStatus.Pending)
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.CreatedAt)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending emails");
                throw;
            }
        }

        public async Task<List<EmailQueue>> GetEmailsByPriorityAsync(EmailPriority priority, int batchSize, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailQueue
                    .Include(e => e.Attachments)
                    .Where(e => e.Status == EmailQueueStatus.Pending && e.Priority == priority)
                    .OrderBy(e => e.CreatedAt)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get emails by priority {Priority}", priority);
                throw;
            }
        }

        public async Task<List<EmailQueue>> GetFailedEmailsForRetryAsync(int maxRetries, CancellationToken cancellationToken = default)
        {
            try
            {
                var retryAfter = DateTime.UtcNow.AddMinutes(-30); // Wait 30 minutes before retry

                return await _context.EmailQueue
                    .Include(e => e.Attachments)
                    .Where(e => e.Status == EmailQueueStatus.Failed &&
                               e.RetryCount < maxRetries &&
                               e.UpdatedAt <= retryAfter)
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.UpdatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get failed emails for retry");
                throw;
            }
        }

        public async Task UpdateEmailStatusAsync(int emailId, EmailQueueStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var email = await _context.EmailQueue.FindAsync(new object[] { emailId }, cancellationToken);
                if (email != null)
                {
                    email.Status = status;
                    email.UpdatedAt = DateTime.UtcNow;

                    if (status == EmailQueueStatus.Processing)
                    {
                        email.ProcessingStartedAt = DateTime.UtcNow;
                    }
                    else if (status == EmailQueueStatus.Sent)
                    {
                        email.SentAt = DateTime.UtcNow;
                    }
                    else if (status == EmailQueueStatus.Failed)
                    {
                        email.ErrorMessage = errorMessage;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update email status for ID {EmailId}", emailId);
                throw;
            }
        }

        public async Task IncrementRetryCountAsync(int emailId, string errorMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var email = await _context.EmailQueue.FindAsync(new object[] { emailId }, cancellationToken);
                if (email != null)
                {
                    email.RetryCount++;
                    email.ErrorMessage = errorMessage;
                    email.UpdatedAt = DateTime.UtcNow;
                    email.Status = EmailQueueStatus.Failed;

                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to increment retry count for email ID {EmailId}", emailId);
                throw;
            }
        }

        public async Task<EmailQueue?> GetByIdAsync(int emailId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailQueue
                    .Include(e => e.Attachments)
                    .FirstOrDefaultAsync(e => e.Id == emailId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email by ID {EmailId}", emailId);
                throw;
            }
        }

        public async Task<EmailQueue> AddAsync(EmailQueue email, CancellationToken cancellationToken = default)
        {
            try
            {
                _context.EmailQueue.Add(email);
                await _context.SaveChangesAsync(cancellationToken);
                return email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add email to queue");
                throw;
            }
        }

        public async Task<QueueStatistics> GetQueueStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _context.EmailQueue
                    .GroupBy(e => e.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                var statistics = new QueueStatistics();

                foreach (var stat in stats)
                {
                    switch (stat.Status)
                    {
                        case EmailQueueStatus.Pending:
                            statistics.PendingCount = stat.Count;
                            break;
                        case EmailQueueStatus.Processing:
                            statistics.ProcessingCount = stat.Count;
                            break;
                        case EmailQueueStatus.Sent:
                            statistics.SentCount = stat.Count;
                            break;
                        case EmailQueueStatus.Failed:
                            statistics.FailedCount = stat.Count;
                            break;
                    }
                }

                statistics.TotalCount = stats.Sum(s => s.Count);
                statistics.LastUpdated = DateTime.UtcNow;

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get queue statistics");
                throw;
            }
        }

        public async Task<int> DeleteOldProcessedEmailsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            try
            {
                var emailsToDelete = await _context.EmailQueue
                    .Where(e => (e.Status == EmailQueueStatus.Sent || e.Status == EmailQueueStatus.Failed) &&
                               e.UpdatedAt < olderThan)
                    .ToListAsync(cancellationToken);

                if (emailsToDelete.Any())
                {
                    _context.EmailQueue.RemoveRange(emailsToDelete);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return emailsToDelete.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old processed emails");
                throw;
            }
        }

        public async Task<List<EmailQueue>> GetEmailsByStatusAsync(EmailQueueStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailQueue
                    .Include(e => e.Attachments)
                    .Where(e => e.Status == status)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get emails by status {Status}", status);
                throw;
            }
        }
    }
}