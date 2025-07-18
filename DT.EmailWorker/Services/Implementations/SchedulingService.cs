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
                    scheduledEmail.NextRunTime, scheduledEmail.Id);

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
                    .Where(se => se.NextRunTime <= now && se.IsActive && (se.ExecutionCount == 0 || se.IsRecurring))
                    .OrderBy(se => se.NextRunTime)
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

                if (scheduledEmail.ExecutionCount > 0 && !scheduledEmail.IsRecurring)
                {
                    _logger.LogWarning("Cannot cancel already processed email - ID: {ScheduledEmailId}", scheduledEmailId);
                    return false;
                }

                scheduledEmail.IsActive = false;
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

                if (scheduledEmail.ExecutionCount > 0 && !scheduledEmail.IsRecurring)
                {
                    _logger.LogWarning("Cannot reschedule already processed email - ID: {ScheduledEmailId}", scheduledEmailId);
                    return false;
                }

                var oldSendTime = scheduledEmail.NextRunTime;
                scheduledEmail.NextRunTime = newSendTime;
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
                    .Where(se => se.NextRunTime >= fromDate && se.NextRunTime <= toDate)
                    .OrderBy(se => se.NextRunTime)
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
                        // Create email processing request instead of EmailQueue entity
                        var emailRequest = new Models.DTOs.EmailProcessingRequest
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
                            Attachments = scheduledEmail.Attachments,
                            CreatedBy = "SchedulingService",
                            RequestSource = "ScheduledEmail"
                        };

                        // Use the correct method name from IEmailQueueService
                        await _emailQueueService.QueueEmailAsync(emailRequest);

                        // Update scheduled email tracking with correct property names
                        scheduledEmail.ExecutionCount++;
                        scheduledEmail.LastExecutedAt = DateTime.UtcNow;
                        scheduledEmail.UpdatedAt = DateTime.UtcNow;

                        // Handle recurring emails
                        if (scheduledEmail.IsRecurring)
                        {
                            UpdateNextRunTime(scheduledEmail);
                        }
                        else
                        {
                            // Non-recurring emails become inactive after execution
                            scheduledEmail.IsActive = false;
                        }

                        processedCount++;

                        _logger.LogDebug("Processed scheduled email - ID: {ScheduledEmailId}",
                            scheduledEmail.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process scheduled email {ScheduledEmailId}", scheduledEmail.Id);

                        // Update error tracking
                        scheduledEmail.LastExecutionError = ex.Message;
                        scheduledEmail.LastExecutionStatus = EmailQueueStatus.Failed;
                        scheduledEmail.UpdatedAt = DateTime.UtcNow;
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

        /// <summary>
        /// Helper method to update next run time for recurring emails
        /// </summary>
        /// <param name="scheduledEmail">The scheduled email to update</param>
        private void UpdateNextRunTime(ScheduledEmail scheduledEmail)
        {
            if (scheduledEmail.IntervalMinutes.HasValue)
            {
                scheduledEmail.NextRunTime = scheduledEmail.NextRunTime.AddMinutes(scheduledEmail.IntervalMinutes.Value);
            }
            else if (!string.IsNullOrEmpty(scheduledEmail.CronExpression))
            {
                // For now, implement a simple daily recurrence
                // You can enhance this later with a proper cron expression parser
                scheduledEmail.NextRunTime = scheduledEmail.NextRunTime.AddDays(1);
            }
            else
            {
                // Default to daily recurrence if no interval or cron expression is specified
                scheduledEmail.NextRunTime = scheduledEmail.NextRunTime.AddDays(1);
            }

            // Check if we've reached the end date or max executions
            if (scheduledEmail.EndDate.HasValue && scheduledEmail.NextRunTime > scheduledEmail.EndDate.Value)
            {
                scheduledEmail.IsActive = false;
            }

            if (scheduledEmail.MaxExecutions.HasValue && scheduledEmail.ExecutionCount >= scheduledEmail.MaxExecutions.Value)
            {
                scheduledEmail.IsActive = false;
            }
        }
    }
}