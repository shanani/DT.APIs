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
                               (!e.IsScheduled || e.ScheduledFor <= DateTime.UtcNow.AddHours(3)))
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
                               e.IsScheduled &&
                               e.ScheduledFor <= DateTime.UtcNow.AddHours(3))
                    .OrderBy(e => e.ScheduledFor)
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
                    email.ProcessingStartedAt = DateTime.UtcNow.AddHours(3);
                    email.ProcessedBy = workerId;
                    email.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    await _context.SaveChangesAsync();

                    _logger.LogDebug("Email {QueueId} marked as processing by worker {WorkerId}",
                        queueId, workerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email {QueueId} as processing", queueId);
                throw;
            }
        }

        // Replace your existing MarkAsSentAsync method with this corrected version:

        public async Task MarkAsSentAsync(Guid queueId, string workerId, int? processingTimeMs = null)
        {
            try
            {
                var email = await _context.EmailQueue
                    .FirstOrDefaultAsync(e => e.QueueId == queueId);

                if (email != null)
                {
                    email.Status = EmailQueueStatus.Sent;
                    email.ProcessedAt = DateTime.UtcNow.AddHours(3);
                    email.ProcessedBy = workerId;
                    email.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Email {QueueId} marked as sent by worker {WorkerId} in {ProcessingTime}ms",
                        queueId, workerId, processingTimeMs ?? 0);
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
                    email.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    // Determine if should retry based on retry count
                    const int maxRetries = 3;
                    if (shouldRetry && email.RetryCount < maxRetries)
                    {
                        email.Status = EmailQueueStatus.Queued; // Put back in queue for retry
                        email.ProcessingStartedAt = null;
                        email.ProcessedBy = null;
                    }
                    else
                    {
                        email.Status = EmailQueueStatus.Failed;
                        email.ProcessedAt = DateTime.UtcNow.AddHours(3);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Email {QueueId} marked as failed. Retry count: {RetryCount}. Will retry: {WillRetry}",
                        queueId, email.RetryCount, shouldRetry && email.RetryCount < maxRetries);
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
                               e.RetryCount < maxRetryCount)
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.CreatedAt)
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
                    Attachments = emailRequest.Attachments, // Already a JSON string
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
                    Attachments = request.Attachments, // Already a JSON string
                    HasEmbeddedImages = request.HasEmbeddedImages,
                    ScheduledFor = request.ScheduledFor,
                    IsScheduled = request.IsScheduled,
                    CreatedBy = request.CreatedBy,
                    RequestSource = request.RequestSource,
                    Status = request.IsScheduled ? EmailQueueStatus.Scheduled : EmailQueueStatus.Queued
                }).ToList();

                _context.EmailQueue.AddRange(queueItems);
                await _context.SaveChangesAsync();

                var queueIds = queueItems.Select(qi => qi.QueueId).ToList();

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

                if (email != null && (email.Status == EmailQueueStatus.Queued || email.Status == EmailQueueStatus.Scheduled))
                {
                    email.Status = EmailQueueStatus.Cancelled;
                    email.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Email {QueueId} cancelled successfully", queueId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling email {QueueId}", queueId);
                return false;
            }
        }

        public async Task<QueueStatistics> GetQueueStatisticsAsync()
        {
            try
            {
                var stats = new QueueStatistics
                {
                    TotalQueued = await _context.EmailQueue.CountAsync(e => e.Status == EmailQueueStatus.Queued),
                    TotalProcessing = await _context.EmailQueue.CountAsync(e => e.Status == EmailQueueStatus.Processing),
                    TotalFailed = await _context.EmailQueue.CountAsync(e => e.Status == EmailQueueStatus.Failed),
                    TotalScheduled = await _context.EmailQueue.CountAsync(e => e.Status == EmailQueueStatus.Scheduled),
                    HighPriorityCount = await _context.EmailQueue.CountAsync(e =>
                        e.Priority == EmailPriority.High &&
                        (e.Status == EmailQueueStatus.Queued || e.Status == EmailQueueStatus.Processing)),
                    NormalPriorityCount = await _context.EmailQueue.CountAsync(e =>
                        e.Priority == EmailPriority.Normal &&
                        (e.Status == EmailQueueStatus.Queued || e.Status == EmailQueueStatus.Processing)),
                    LowPriorityCount = await _context.EmailQueue.CountAsync(e =>
                        e.Priority == EmailPriority.Low &&
                        (e.Status == EmailQueueStatus.Queued || e.Status == EmailQueueStatus.Processing))
                };

                // Get oldest queued email
                var oldestQueued = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Queued)
                    .OrderBy(e => e.CreatedAt)
                    .FirstOrDefaultAsync();

                if (oldestQueued != null)
                {
                    stats.OldestQueuedEmail = oldestQueued.CreatedAt;
                    stats.AverageQueueTimeHours = (DateTime.UtcNow.AddHours(3) - oldestQueued.CreatedAt).TotalHours;
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
                var threshold = DateTime.UtcNow.AddHours(3).AddMinutes(-stuckThresholdMinutes);

                var stuckEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Processing &&
                               e.ProcessingStartedAt <= threshold)
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
                var threshold = DateTime.UtcNow.AddHours(3).AddMinutes(-stuckThresholdMinutes);

                var stuckEmails = await _context.EmailQueue
                    .Where(e => e.Status == EmailQueueStatus.Processing &&
                               e.ProcessingStartedAt <= threshold)
                    .ToListAsync();

                foreach (var email in stuckEmails)
                {
                    email.Status = EmailQueueStatus.Queued;
                    email.ProcessingStartedAt = null;
                    email.ProcessedBy = null;
                    email.UpdatedAt = DateTime.UtcNow.AddHours(3);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reset {Count} stuck emails to queued status", stuckEmails.Count);

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

                if (email != null && (email.Status == EmailQueueStatus.Queued || email.Status == EmailQueueStatus.Scheduled))
                {
                    email.Priority = priority;
                    email.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Email {QueueId} priority updated to {Priority}", queueId, priority);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email priority for {QueueId}", queueId);
                return false;
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
                    email.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Email {QueueId} scheduled for {ScheduledFor}", queueId, scheduledFor);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling email {QueueId}", queueId);
                return false;
            }
        }

        private EmailProcessingRequest ConvertToProcessingRequest(EmailQueue queueItem)
        {
            return new EmailProcessingRequest
            {
                QueueId = queueItem.QueueId,
                Priority = queueItem.Priority,
                ToEmails = queueItem.ToEmails,
                CcEmails = queueItem.CcEmails,
                BccEmails = queueItem.BccEmails,
                Subject = queueItem.Subject,
                Body = queueItem.Body,
                IsHtml = queueItem.IsHtml,
                TemplateId = queueItem.TemplateId,
                TemplateData = queueItem.TemplateData,
                RequiresTemplateProcessing = queueItem.RequiresTemplateProcessing,
                Attachments = queueItem.Attachments, // This is already a JSON string
                HasEmbeddedImages = queueItem.HasEmbeddedImages,
                ScheduledFor = queueItem.ScheduledFor,
                IsScheduled = queueItem.IsScheduled,
                CreatedBy = queueItem.CreatedBy,
                RequestSource = queueItem.RequestSource
            };
        }
    }
}