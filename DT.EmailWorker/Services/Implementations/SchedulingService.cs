using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// Implementation of scheduled email service
    /// </summary>
    public class SchedulingService : ISchedulingService
    {
        private readonly EmailDbContext _context;
        private readonly IEmailQueueService _emailQueueService;
        private readonly ILogger<SchedulingService> _logger;

        public SchedulingService(
            EmailDbContext context,
            IEmailQueueService emailQueueService,
            ILogger<SchedulingService> logger)
        {
            _context = context;
            _emailQueueService = emailQueueService;
            _logger = logger;
        }

        public async Task<ScheduledEmail> ScheduleEmailAsync(ScheduledEmail scheduledEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                scheduledEmail.CreatedAt = DateTime.UtcNow;
                scheduledEmail.UpdatedAt = DateTime.UtcNow;

                _context.ScheduledEmails.Add(scheduledEmail);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Email scheduled for {SendTime} - ID: {ScheduledEmailId}",
                    scheduledEmail.ScheduledSendTime, scheduledEmail.Id);

                return scheduledEmail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule email");
                throw;
            }
        }

        public async Task<List<ScheduledEmail>> GetDueEmailsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;

                return await _context.ScheduledEmails
                    .Where(se => se.ScheduledSendTime <= now && !se.IsProcessed && !se.IsCancelled)
                    .OrderBy(se => se.ScheduledSendTime)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get due emails");
                throw;
            }
        }

        public async Task<bool> CancelScheduledEmailAsync(int scheduledEmailId, CancellationToken cancellationToken = default)
        {
            try
            {
                var scheduledEmail = await _context.ScheduledEmails.FindAsync(new object[] { scheduledEmailId }, cancellationToken);
                if (scheduledEmail == null)
                {
                    _logger.LogWarning("Scheduled email not found for cancellation - ID: {ScheduledEmailId}", scheduledEmailId);
                    return false;
                }

                if (scheduledEmail.IsProcessed)
                {
                    _logger.LogWarning("Cannot cancel already processed email - ID: {ScheduledEmailId}", scheduledEmailId);
                    return false;
                }

                scheduledEmail.IsCancelled = true;
                scheduledEmail.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Scheduled email cancelled - ID: {ScheduledEmailId}", scheduledEmailId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel scheduled email {ScheduledEmailId}", scheduledEmailId);
                return false;
            }
        }

        public async Task<bool> RescheduleEmailAsync(int scheduledEmailId, DateTime newSendTime, CancellationToken cancellationToken = default)
        {
            try
            {
                var scheduledEmail = await _context.ScheduledEmails.FindAsync(new object[] { scheduledEmailId }, cancellationToken);
                if (scheduledEmail == null)
                {
                    _logger.LogWarning("Scheduled email not found for rescheduling - ID: {ScheduledEmailId}", scheduledEmailId);
                    return false;
                }

                if (scheduledEmail.IsProcessed)
                {
                    _logger.LogWarning("Cannot reschedule already processed email - ID: {ScheduledEmailId}", scheduledEmailId);
                    return false;
                }

                var oldSendTime = scheduledEmail.ScheduledSendTime;
                scheduledEmail.ScheduledSendTime = newSendTime;
                scheduledEmail.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Scheduled email rescheduled from {OldTime} to {NewTime} - ID: {ScheduledEmailId}",
                    oldSendTime, newSendTime, scheduledEmailId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reschedule email {ScheduledEmailId}", scheduledEmailId);
                return false;
            }
        }

        public async Task<List<ScheduledEmail>> GetScheduledEmailsByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ScheduledEmails
                    .Where(se => se.ScheduledSendTime >= fromDate && se.ScheduledSendTime <= toDate)
                    .OrderBy(se => se.ScheduledSendTime)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get scheduled emails by date range");
                throw;
            }
        }

        public async Task<int> ProcessDueEmailsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var dueEmails = await GetDueEmailsAsync(cancellationToken);
                var processedCount = 0;

                _logger.LogInformation("Processing {Count} due scheduled emails", dueEmails.Count);

                foreach (var scheduledEmail in dueEmails)
                {
                    try
                    {
                        // Create regular email queue item from scheduled email
                        var queueItem = new EmailQueue
                        {
                            ToEmails = scheduledEmail.ToEmails,
                            CcEmails = scheduledEmail.CcEmails,
                            BccEmails = scheduledEmail.BccEmails,
                            Subject = scheduledEmail.Subject,
                            Body = scheduledEmail.Body,
                            IsHtml = scheduledEmail.IsHtml,
                            Priority = scheduledEmail.Priority,
                            TemplateId = scheduledEmail.TemplateId,
                            TemplateData = scheduledEmail.TemplateData,
                            CreatedBy = "SchedulingService",
                            Status = EmailQueueStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _emailQueueService.AddEmailToQueueAsync(queueItem, cancellationToken);

                        // Mark scheduled email as processed
                        scheduledEmail.IsProcessed = true;
                        scheduledEmail.ProcessedAt = DateTime.UtcNow;
                        scheduledEmail.UpdatedAt = DateTime.UtcNow;

                        processedCount++;

                        _logger.LogDebug("Processed scheduled email - ID: {ScheduledEmailId}, Queue ID: {QueueId}",
                            scheduledEmail.Id, queueItem.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process scheduled email {ScheduledEmailId}", scheduledEmail.Id);
                    }
                }

                if (processedCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Processed {ProcessedCount}/{TotalCount} scheduled emails",
                    processedCount, dueEmails.Count);

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process due emails");
                throw;
            }
        }
    }
}