using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Core.Utilities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DT.EmailWorker.Workers
{
    /// <summary>
    /// Main background worker for processing email queue
    /// </summary>
    public class EmailProcessingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailWorkerSettings _settings;
        private readonly ProcessingSettings _processingSettings;
        private readonly ILogger<EmailProcessingWorker> _logger;
        private readonly SemaphoreSlim _semaphore;
        private int _processingCount = 0;
        private DateTime _lastProcessingTime = DateTime.UtcNow;

        public EmailProcessingWorker(
            IServiceProvider serviceProvider,
            IOptions<EmailWorkerSettings> settings,
            IOptions<ProcessingSettings> processingSettings,
            ILogger<EmailProcessingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _processingSettings = processingSettings.Value;
            _logger = logger;
            _semaphore = new SemaphoreSlim(_processingSettings.MaxConcurrentWorkers, _processingSettings.MaxConcurrentWorkers);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoggingHelper.LogWorkerStart(_logger, "Email Processing Worker");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessEmailBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error in email processing worker");
                }

                // Wait for the configured polling interval
                var delay = TimeSpan.FromSeconds(_settings.PollingIntervalSeconds);
                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("Email Processing Worker stopped");
        }

        private async Task ProcessEmailBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
            var emailProcessingService = scope.ServiceProvider.GetRequiredService<IEmailProcessingService>();

            try
            {
                // Get pending emails from queue
                var pendingEmails = await emailQueueService.GetPendingEmailsAsync(_processingSettings.BatchSize, cancellationToken);

                if (!pendingEmails.Any())
                {
                    _logger.LogDebug("No pending emails found in queue");
                    return;
                }

                _logger.LogInformation("Processing batch of {EmailCount} emails", pendingEmails.Count);
                var batchStopwatch = Stopwatch.StartNew();

                // Process emails with parallel execution
                var semaphore = new SemaphoreSlim(_processingSettings.MaxConcurrentWorkers, _processingSettings.MaxConcurrentWorkers);
                var processingTasks = pendingEmails.Select(async email =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await ProcessSingleEmailAsync(email, emailQueueService, emailProcessingService, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                        Interlocked.Decrement(ref _processingCount);
                    }
                });

                Interlocked.Add(ref _processingCount, pendingEmails.Count);
                await Task.WhenAll(processingTasks);

                batchStopwatch.Stop();
                _lastProcessingTime = DateTime.UtcNow;

                LoggingHelper.LogBatchProcessingComplete(_logger, pendingEmails.Count, 0, batchStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email batch");
            }
        }

        private async Task ProcessSingleEmailAsync(
            Models.Entities.EmailQueue emailQueue,
            IEmailQueueService emailQueueService,
            IEmailProcessingService emailProcessingService,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                LoggingHelper.LogEmailProcessingStart(_logger, emailQueue.Id, emailQueue.ToEmails, emailQueue.Subject);

                // Update status to processing
                await emailQueueService.UpdateEmailStatusAsync(emailQueue.Id, EmailQueueStatus.Processing, cancellationToken);

                // Create processing request
                var processingRequest = CreateProcessingRequest(emailQueue);

                // Process the email
                var result = await emailProcessingService.ProcessEmailAsync(processingRequest, cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    // Mark as sent
                    await emailQueueService.UpdateEmailStatusAsync(emailQueue.Id, EmailQueueStatus.Sent, cancellationToken);
                    LoggingHelper.LogEmailProcessingSuccess(_logger, emailQueue.Id, emailQueue.ToEmails, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    // Handle failure
                    await HandleEmailProcessingFailure(emailQueue, result.ErrorMessage, emailQueueService, cancellationToken);
                    LoggingHelper.LogEmailProcessingFailure(_logger, emailQueue.Id, emailQueue.ToEmails,
                        new Exception(result.ErrorMessage), emailQueue.RetryCount);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await HandleEmailProcessingFailure(emailQueue, ex.Message, emailQueueService, cancellationToken);
                LoggingHelper.LogEmailProcessingFailure(_logger, emailQueue.Id, emailQueue.ToEmails, ex, emailQueue.RetryCount);
            }
        }

        private Models.DTOs.EmailProcessingRequest CreateProcessingRequest(Models.Entities.EmailQueue emailQueue)
        {
            return new Models.DTOs.EmailProcessingRequest
            {
                QueueId = emailQueue.Id,
                ToEmails = emailQueue.ToEmails,
                CcEmails = emailQueue.CcEmails,
                BccEmails = emailQueue.BccEmails,
                Subject = emailQueue.Subject,
                Body = emailQueue.Body,
                IsHtml = emailQueue.IsHtml,
                Priority = emailQueue.Priority,
                TemplateId = emailQueue.TemplateId,
                TemplateData = emailQueue.TemplateData,
                Attachments = emailQueue.Attachments?.Select(a => new Models.DTOs.AttachmentData
                {
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    Content = a.Content,
                    FilePath = a.FilePath
                }).ToList() ?? new List<Models.DTOs.AttachmentData>(),
                CreatedBy = emailQueue.CreatedBy,
                CreatedAt = emailQueue.CreatedAt
            };
        }

        private async Task HandleEmailProcessingFailure(
            Models.Entities.EmailQueue emailQueue,
            string? errorMessage,
            IEmailQueueService emailQueueService,
            CancellationToken cancellationToken)
        {
            try
            {
                // Check if we should retry
                if (emailQueue.RetryCount < _processingSettings.MaxRetryAttempts)
                {
                    // Increment retry count and set back to pending with delay
                    await emailQueueService.IncrementRetryCountAsync(emailQueue.Id, errorMessage ?? "Unknown error", cancellationToken);

                    _logger.LogWarning("Email {EmailId} failed, will retry. Attempt {RetryCount}/{MaxRetries}",
                        emailQueue.Id, emailQueue.RetryCount + 1, _processingSettings.MaxRetryAttempts);
                }
                else
                {
                    // Max retries exceeded, mark as failed
                    await emailQueueService.UpdateEmailStatusAsync(emailQueue.Id, EmailQueueStatus.Failed,
                        $"Max retries exceeded. Last error: {errorMessage}", cancellationToken);

                    _logger.LogError("Email {EmailId} failed permanently after {RetryCount} attempts. Error: {ErrorMessage}",
                        emailQueue.Id, emailQueue.RetryCount, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling email processing failure for email {EmailId}", emailQueue.Id);
            }
        }

        /// <summary>
        /// Get current processing statistics
        /// </summary>
        public ProcessingWorkerStats GetCurrentStats()
        {
            return new ProcessingWorkerStats
            {
                IsRunning = !_disposed,
                CurrentlyProcessing = _processingCount,
                MaxConcurrentWorkers = _processingSettings.MaxConcurrentWorkers,
                AvailableWorkerSlots = _semaphore.CurrentCount,
                LastProcessingTime = _lastProcessingTime,
                UtilizationPercentage = ((double)(_processingSettings.MaxConcurrentWorkers - _semaphore.CurrentCount) / _processingSettings.MaxConcurrentWorkers) * 100
            };
        }

        /// <summary>
        /// Process retry emails (failed emails that are eligible for retry)
        /// </summary>
        public async Task ProcessRetryEmailsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            try
            {
                var retryEmails = await emailQueueService.GetFailedEmailsForRetryAsync(_processingSettings.MaxRetryAttempts, cancellationToken);

                if (retryEmails.Any())
                {
                    _logger.LogInformation("Processing {RetryCount} emails eligible for retry", retryEmails.Count);

                    foreach (var email in retryEmails)
                    {
                        // Reset status to pending for retry
                        await emailQueueService.UpdateEmailStatusAsync(email.Id, EmailQueueStatus.Pending, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing retry emails");
            }
        }

        /// <summary>
        /// Force process specific email by ID
        /// </summary>
        public async Task<bool> ForceProcessEmailAsync(int emailId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
            var emailProcessingService = scope.ServiceProvider.GetRequiredService<IEmailProcessingService>();

            try
            {
                var email = await emailQueueService.GetEmailByIdAsync(emailId, cancellationToken);
                if (email == null)
                {
                    _logger.LogWarning("Email {EmailId} not found for force processing", emailId);
                    return false;
                }

                _logger.LogInformation("Force processing email {EmailId}", emailId);
                await ProcessSingleEmailAsync(email, emailQueueService, emailProcessingService, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force processing email {EmailId}", emailId);
                return false;
            }
        }

        /// <summary>
        /// Pause email processing
        /// </summary>
        public void PauseProcessing()
        {
            // Implementation would set a pause flag
            _logger.LogInformation("Email processing paused");
        }

        /// <summary>
        /// Resume email processing
        /// </summary>
        public void ResumeProcessing()
        {
            // Implementation would clear pause flag
            _logger.LogInformation("Email processing resumed");
        }

        private bool _disposed = false;

        public override void Dispose()
        {
            _disposed = true;
            _semaphore?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Processing worker statistics
    /// </summary>
    public class ProcessingWorkerStats
    {
        public bool IsRunning { get; set; }
        public int CurrentlyProcessing { get; set; }
        public int MaxConcurrentWorkers { get; set; }
        public int AvailableWorkerSlots { get; set; }
        public DateTime LastProcessingTime { get; set; }
        public double UtilizationPercentage { get; set; }
    }
}

// Add the missing DTOs that are referenced
namespace DT.EmailWorker.Models.DTOs
{
    public class AttachmentData
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? FilePath { get; set; }
    }
}