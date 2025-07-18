using DT.EmailWorker.Data;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// Implementation of email queue management service
    /// </summary>
    public class EmailQueueService : IEmailQueueService
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<EmailQueueService> _logger;

        public EmailQueueService(EmailDbContext context, ILogger<EmailQueueService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<EmailProcessingRequest>> GetPendingEmailsAsync(int batchSize, string workerId)
        {
            try
            {
                var pendingEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Queued &&
                               (!e.IsScheduled || e.ScheduledFor <= DateTime.UtcNow))
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.CreatedAt)
                    .Take(batchSize)
                    .ToListAsync();

                var requests = pendingEmails.Select(ConvertToProcessingRequest).ToList();

                _logger.LogInformation("Retrieved {Count} pending emails for worker {WorkerId}",
                    requests.Count, workerId);

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending emails for worker {WorkerId}", workerId);
                throw;
            }
        }

        public async Task<List<EmailProcessingRequest>> GetDueScheduledEmailsAsync(int batchSize)
        {
            try
            {
                var dueEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Scheduled &&
                               e.ScheduledFor <= DateTime.UtcNow)
                    .OrderBy(e => e.ScheduledFor)
                    .ThenBy(e => e.Priority)
                    .Take(batchSize)
                    .ToListAsync();

                var requests = dueEmails.Select(ConvertToProcessingRequest).ToList();

                _logger.LogInformation("Retrieved {Count} due scheduled emails", requests.Count);

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving due scheduled emails");
                throw;
            }
        }

        public async Task MarkAsProcessingAsync(Guid queueId, string workerId)
        {
            try
            {
                var email = await _context.EmailQueue
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);

                if (email != null)
                {
                    email.Status = EmailQueueStatus.Processing;
                    email.ProcessingStartedAt = DateTime.UtcNow;
                    email.ProcessedBy = workerId;

                    await _context.SaveChangesAsync();

                    _logger.LogDebug("Marked email {QueueId} as processing by worker {WorkerId}",
                        queueId, workerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email {QueueId} as processing", queueId);
                throw;
            }
        }

        public async Task MarkAsSentAsync(Guid queueId, string workerId, int processingTimeMs)
        {
            try
            {
                var email = await _context.EmailQueue
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);

                if (email != null)
                {
                    email.Status = EmailQueueStatus.Sent;
                    email.ProcessedAt = DateTime.UtcNow;
                    email.ProcessedBy = workerId;

                    // Create history record
                    var history = new EmailHistory
                    {
                        QueueId = email.QueueId,
                        ToEmails = email.ToEmails,
                        CcEmails = email.CcEmails,
                        BccEmails = email.BccEmails,
                        Subject = email.Subject,
                        FinalBody = email.Body,
                        Status = EmailQueueStatus.Sent,
                        SentAt = DateTime.UtcNow,
                        TemplateId = email.TemplateId,
                        TemplateUsed = email.Template?.Name,
                        ProcessingTimeMs = processingTimeMs,
                        RetryCount = email.RetryCount,
                        ProcessedBy = workerId
                    };

                    _context.EmailHistory.Add(history);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Email {QueueId} sent successfully by worker {WorkerId} in {ProcessingTime}ms",
                        queueId, workerId, processingTimeMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email {QueueId} as sent", queueId);
                throw;
            }
        }

        public async Task MarkAsFailedAsync(Guid queueId, string errorMessage, bool shouldRetry = true)
        {
            try
            {
                var email = await _context.EmailQueue
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);

                if (email != null)
                {
                    email.RetryCount++;
                    email.ErrorMessage = errorMessage;
                    email.ProcessedAt = DateTime.UtcNow;

                    if (shouldRetry && email.RetryCount < 3) // Max retry attempts
                    {
                        email.Status = EmailQueueStatus.Queued;
                        email.ProcessingStartedAt = null;
                        // Reset for retry after delay
                        email.ScheduledFor = DateTime.UtcNow.AddMinutes(email.RetryCount * 5);
                    }
                    else
                    {
                        email.Status = EmailQueueStatus.Failed;

                        // Create history record for failed email
                        var history = new EmailHistory
                        {
                            QueueId = email.QueueId,
                            ToEmails = email.ToEmails,
                            CcEmails = email.CcEmails,
                            BccEmails = email.BccEmails,
                            Subject = email.Subject,
                            FinalBody = email.Body,
                            Status = EmailQueueStatus.Failed,
                            TemplateId = email.TemplateId,
                            TemplateUsed = email.Template?.Name,
                            RetryCount = email.RetryCount,
                            ErrorDetails = errorMessage,
                            ProcessedBy = email.ProcessedBy
                        };

                        _context.EmailHistory.Add(history);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Email {QueueId} failed (retry {RetryCount}): {ErrorMessage}",
                        queueId, email.RetryCount, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email {QueueId} as failed", queueId);
                throw;
            }
        }

        public async Task<List<EmailProcessingRequest>> GetEmailsForRetryAsync(int batchSize, int maxRetryCount)
        {
            try
            {
                var retryEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Queued &&
                               e.RetryCount > 0 &&
                               e.RetryCount < maxRetryCount &&
                               (!e.ScheduledFor.HasValue || e.ScheduledFor <= DateTime.UtcNow))
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.ScheduledFor)
                    .Take(batchSize)
                    .ToListAsync();

                var requests = retryEmails.Select(ConvertToProcessingRequest).ToList();

                _logger.LogInformation("Retrieved {Count} emails for retry", requests.Count);

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emails for retry");
                throw;
            }
        }

        public async Task<Guid> QueueEmailAsync(EmailProcessingRequest emailRequest)
        {
            try
            {
                var queueItem = new EmailQueue
                {
                    QueueId = emailRequest.QueueId == Guid.Empty ? Guid.NewGuid() : emailRequest.QueueId,
                    Priority = emailRequest.Priority,
                    ToEmails = emailRequest.ToEmails,
                    CcEmails = emailRequest.CcEmails,
                    BccEmails = emailRequest.BccEmails,
                    Subject = emailRequest.Subject,
                    Body = emailRequest.Body,
                    IsHtml = emailRequest.IsHtml,
                    TemplateId = emailRequest.TemplateId,
                    TemplateData = emailRequest.TemplateData,
                    RequiresTemplateProcessing = emailRequest.RequiresTemplateProcessing,
                    Attachments = emailRequest.Attachments,
                    HasEmbeddedImages = emailRequest.HasEmbeddedImages,
                    ScheduledFor = emailRequest.ScheduledFor,
                    IsScheduled = emailRequest.IsScheduled,
                    CreatedBy = emailRequest.CreatedBy,
                    RequestSource = emailRequest.RequestSource,
                    Status = emailRequest.IsScheduled ? EmailQueueStatus.Scheduled : EmailQueueStatus.Queued
                };

                _context.EmailQueue.Add(queueItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Email queued successfully with ID {QueueId}", queueItem.QueueId);

                return queueItem.QueueId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing email");
                throw;
            }
        }

        public async Task<List<Guid>> QueueBulkEmailsAsync(List<EmailProcessingRequest> emailRequests)
        {
            try
            {
                var queueItems = emailRequests.Select(request => new EmailQueue
                {
                    QueueId = request.QueueId == Guid.Empty ? Guid.NewGuid() : request.QueueId,
                    Priority = request.Priority,
                    ToEmails = request.ToEmails,
                    CcEmails = request.CcEmails,
                    BccEmails = request.BccEmails,
                    Subject = request.Subject,
                    Body = request.Body,
                    IsHtml = request.IsHtml,
                    TemplateId = request.TemplateId,
                    TemplateData = request.TemplateData,
                    RequiresTemplateProcessing = request.RequiresTemplateProcessing,
                    Attachments = request.Attachments,
                    HasEmbeddedImages = request.HasEmbeddedImages,
                    ScheduledFor = request.ScheduledFor,
                    IsScheduled = request.IsScheduled,
                    CreatedBy = request.CreatedBy,
                    RequestSource = request.RequestSource,
                    Status = request.IsScheduled ? EmailQueueStatus.Scheduled : EmailQueueStatus.Queued
                }).ToList();

                _context.EmailQueue.AddRange(queueItems);
                await _context.SaveChangesAsync();

                var queueIds = queueItems.Select(q => q.QueueId).ToList();

                _logger.LogInformation("Bulk queued {Count} emails", queueIds.Count);

                return queueIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk queueing emails");
                throw;
            }
        }

        public async Task<bool> CancelEmailAsync(Guid queueId)
        {
            try
            {
                var email = await _context.EmailQueue
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);

                if (email != null &&
                    (email.Status == EmailQueueStatus.Queued || email.Status == EmailQueueStatus.Scheduled))
                {
                    email.Status = EmailQueueStatus.Cancelled;
                    email.ProcessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Email {QueueId} cancelled successfully", queueId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling email {QueueId}", queueId);
                throw;
            }
        }

        public async Task<QueueStatistics> GetQueueStatisticsAsync()
        {
            try
            {
                var stats = new QueueStatistics();

                var queueCounts = await _context.EmailQueue
                    .GroupBy(e => e.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var count in queueCounts)
                {
                    switch (count.Status)
                    {
                        case EmailQueueStatus.Queued:
                            stats.TotalQueued = count.Count;
                            break;
                        case EmailQueueStatus.Processing:
                            stats.TotalProcessing = count.Count;
                            break;
                        case EmailQueueStatus.Failed:
                            stats.TotalFailed = count.Count;
                            break;
                        case EmailQueueStatus.Scheduled:
                            stats.TotalScheduled = count.Count;
                            break;
                    }
                }

                var priorityCounts = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Queued)
                    .GroupBy(e => e.Priority)
                    .Select(g => new { Priority = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var count in priorityCounts)
                {
                    switch (count.Priority)
                    {
                        case EmailPriority.High:
                            stats.HighPriorityCount = count.Count;
                            break;
                        case EmailPriority.Normal:
                            stats.NormalPriorityCount = count.Count;
                            break;
                        case EmailPriority.Low:
                            stats.LowPriorityCount = count.Count;
                            break;
                    }
                }

                var oldestQueued = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Queued)
                    .OrderBy(e => e.CreatedAt)
                    .Select(e => e.CreatedAt)
                    .FirstOrDefaultAsync();

                stats.OldestQueuedEmail = oldestQueued;

                if (stats.TotalQueued > 0)
                {
                    var avgQueueTime = await _context.EmailQueue
                        .Where(e => e.Status == EmailQueueStatus.Queued)
                        .AverageAsync(e => EF.Functions.DateDiffMinute(e.CreatedAt, DateTime.UtcNow));

                    stats.AverageQueueTimeHours = avgQueueTime / 60.0;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue statistics");
                throw;
            }
        }

        public async Task<List<EmailQueue>> GetStuckEmailsAsync(int stuckThresholdMinutes)
        {
            try
            {
                var stuckTime = DateTime.UtcNow.AddMinutes(-stuckThresholdMinutes);

                var stuckEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Processing &&
                               e.ProcessingStartedAt < stuckTime)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} stuck emails", stuckEmails.Count);

                return stuckEmails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stuck emails");
                throw;
            }
        }

        public async Task<int> ResetStuckEmailsAsync(int stuckThresholdMinutes)
        {
            try
            {
                var stuckTime = DateTime.UtcNow.AddMinutes(-stuckThresholdMinutes);

                var stuckEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Processing &&
                               e.ProcessingStartedAt < stuckTime)
                    .ToListAsync();

                foreach (var email in stuckEmails)
                {
                    email.Status = EmailQueueStatus.Queued;
                    email.ProcessingStartedAt = null;
                    email.ProcessedBy = null;
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning("Reset {Count} stuck emails to queued status", stuckEmails.Count);

                return stuckEmails.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting stuck emails");
                throw;
            }
        }

        public async Task<EmailQueue?> GetEmailByQueueIdAsync(Guid queueId)
        {
            try
            {
                return await _context.EmailQueue
                    .Include(e => e.Template)
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email by queue ID {QueueId}", queueId);
                throw;
            }
        }

        public async Task<bool> UpdateEmailPriorityAsync(Guid queueId, EmailPriority priority)
        {
            try
            {
                var email = await _context.EmailQueue
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);

                if (email != null && email.Status == EmailQueueStatus.Queued)
                {
                    email.Priority = priority;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated email {QueueId} priority to {Priority}",
                        queueId, priority);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email priority for {QueueId}", queueId);
                throw;
            }
        }

        public async Task<bool> ScheduleEmailAsync(Guid queueId, DateTime scheduledFor)
        {
            try
            {
                var email = await _context.EmailQueue
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);

                if (email != null && email.Status == EmailQueueStatus.Queued)
                {
                    email.ScheduledFor = scheduledFor;
                    email.IsScheduled = true;
                    email.Status = EmailQueueStatus.Scheduled;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Scheduled email {QueueId} for {ScheduledFor}",
                        queueId, scheduledFor);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling email {QueueId}", queueId);
                throw;
            }
        }

        private static EmailProcessingRequest ConvertToProcessingRequest(EmailQueue email)
        {
            return new EmailProcessingRequest
            {
                QueueId = email.QueueId,
                Priority = email.Priority,
                ToEmails = email.ToEmails,
                CcEmails = email.CcEmails,
                BccEmails = email.BccEmails,
                Subject = email.Subject,
                Body = email.Body,
                IsHtml = email.IsHtml,
                TemplateId = email.TemplateId,
                TemplateData = email.TemplateData,
                RequiresTemplateProcessing = email.RequiresTemplateProcessing,
                Attachments = email.Attachments,
                HasEmbeddedImages = email.HasEmbeddedImages,
                RetryCount = email.RetryCount,
                ScheduledFor = email.ScheduledFor,
                IsScheduled = email.IsScheduled,
                CreatedAt = email.CreatedAt,
                CreatedBy = email.CreatedBy,
                RequestSource = email.RequestSource
            };
        }
    }
}