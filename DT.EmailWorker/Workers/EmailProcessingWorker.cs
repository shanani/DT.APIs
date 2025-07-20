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
        private DateTime _lastProcessingTime = DateTime.UtcNow.AddHours(3);

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

        // CRITICAL FIX: Each email processing gets its own scope to prevent DbContext conflicts

        private async Task ProcessSingleEmailAsync(
            Models.DTOs.EmailProcessingRequest emailRequest,
            CancellationToken cancellationToken)
        {
            // 🔥 CRITICAL FIX: Create separate scope for EACH email to prevent DbContext conflicts
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
            var emailProcessingService = scope.ServiceProvider.GetRequiredService<IEmailProcessingService>();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var emailId = (int)(emailRequest.QueueId.GetHashCode() & 0x7FFFFFFF);
                LoggingHelper.LogEmailProcessingStart(_logger, emailId, emailRequest.ToEmails, emailRequest.Subject);

                var workerId = Environment.MachineName + "-" + Thread.CurrentThread.ManagedThreadId;
                await emailQueueService.MarkAsProcessingAsync(emailRequest.QueueId, workerId);

                // Process the email
                var result = await emailProcessingService.ProcessEmailAsync(emailRequest, cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    await emailQueueService.MarkAsSentAsync(emailRequest.QueueId, workerId, null);
                    LoggingHelper.LogEmailProcessingSuccess(_logger, emailId, emailRequest.ToEmails, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    await HandleEmailProcessingFailure(emailRequest, result.ErrorMessage, emailQueueService, cancellationToken);
                    LoggingHelper.LogEmailProcessingFailure(_logger, emailId, emailRequest.ToEmails,
                        new Exception(result.ErrorMessage ?? "Unknown error"), 0);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var emailId = (int)(emailRequest.QueueId.GetHashCode() & 0x7FFFFFFF);
                LoggingHelper.LogEmailProcessingFailure(_logger, emailId, emailRequest.ToEmails, ex, 0);
                await HandleEmailProcessingFailure(emailRequest, ex.Message, emailQueueService, cancellationToken);
            }
        }

        private async Task ProcessEmailBatchAsync(CancellationToken cancellationToken)
        {
            // 🔥 CRITICAL FIX: Only use scope for fetching emails, not for processing them
            using var fetchScope = _serviceProvider.CreateScope();
            var emailQueueService = fetchScope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            try
            {
                var workerId = Environment.MachineName + "-" + Thread.CurrentThread.ManagedThreadId;
                var pendingEmails = await emailQueueService.GetPendingEmailsAsync(_processingSettings.BatchSize, workerId);

                if (!pendingEmails.Any())
                {
                    _logger.LogDebug("No pending emails found");
                    return;
                }

                var batchStopwatch = Stopwatch.StartNew();
                LoggingHelper.LogBatchProcessingStart(_logger, pendingEmails.Count, "Normal");

                // 🔥 CRITICAL FIX: Each task gets its own scope via ProcessSingleEmailAsync
                var processingTasks = pendingEmails.Select(async emailRequest =>
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        Interlocked.Increment(ref _processingCount);
                        // ✅ This now creates its own scope internally - FIXED: Only 2 parameters needed
                        await ProcessSingleEmailAsync(emailRequest, cancellationToken);
                    }
                    finally
                    {
                        _semaphore.Release();
                        Interlocked.Decrement(ref _processingCount);
                    }
                });

                await Task.WhenAll(processingTasks);

                batchStopwatch.Stop();
                _lastProcessingTime = DateTime.UtcNow.AddHours(3);

                LoggingHelper.LogBatchProcessingComplete(_logger, pendingEmails.Count, pendingEmails.Count, 0, batchStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email batch");
            }
        }

        private async Task HandleEmailProcessingFailure(
            Models.DTOs.EmailProcessingRequest emailRequest,
            string? errorMessage,
            IEmailQueueService emailQueueService,
            CancellationToken cancellationToken)
        {
            try
            {
                await emailQueueService.MarkAsFailedAsync(emailRequest.QueueId, errorMessage ?? "Unknown error", true);
                _logger.LogWarning("Email {QueueId} failed: {ErrorMessage}", emailRequest.QueueId, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling email processing failure for {QueueId}", emailRequest.QueueId);
            }
        }

        // Overload method for when we already have the services scoped
        private async Task HandleEmailProcessingFailure(
            Models.DTOs.EmailProcessingRequest emailRequest,
            string? errorMessage,
            CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            await HandleEmailProcessingFailure(emailRequest, errorMessage, emailQueueService, cancellationToken);
        }


      
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // FIX: Use simple logging instead of missing LogWorkerStart method
            _logger.LogInformation("Email Processing Worker started");

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

                // Wait between processing cycles
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Email Processing Worker stopped");
        }
         


        /// <summary>
        /// Process retry emails
        /// </summary>
        public async Task ProcessRetryEmailsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            try
            {
                // FIX: Use GetStuckEmailsAsync and reset them - use 30 minutes as default threshold
                var stuckThresholdMinutes = 30; // Default stuck email threshold
                var stuckEmails = await emailQueueService.GetStuckEmailsAsync(stuckThresholdMinutes);

                if (stuckEmails.Any())
                {
                    _logger.LogInformation("Processing {RetryCount} stuck emails for retry", stuckEmails.Count);

                    // Reset stuck emails to queued status
                    await emailQueueService.ResetStuckEmailsAsync(stuckThresholdMinutes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing retry emails");
            }
        }

        /// <summary>
        /// Force process specific email by queue ID
        /// </summary>
        public async Task<bool> ForceProcessEmailAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

            try
            {
                // FIX: Use GetEmailByQueueIdAsync instead of GetEmailByIdAsync
                var email = await emailQueueService.GetEmailByQueueIdAsync(queueId);
                if (email == null)
                {
                    _logger.LogWarning("Email {QueueId} not found for force processing", queueId);
                    return false;
                }

                _logger.LogInformation("Force processing email {QueueId}", queueId);

                // Convert EmailQueue entity to EmailProcessingRequest
                var emailRequest = ConvertToProcessingRequest(email);

                // 🔥 FIX: Use the new ProcessSingleEmailAsync that creates its own scope
                await ProcessSingleEmailAsync(emailRequest, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force processing email {QueueId}", queueId);
                return false;
            }
        }

        /// <summary>
        /// Convert EmailQueue entity to EmailProcessingRequest DTO
        /// </summary>
        private Models.DTOs.EmailProcessingRequest ConvertToProcessingRequest(Models.Entities.EmailQueue emailQueue)
        {
            return new Models.DTOs.EmailProcessingRequest
            {
                QueueId = emailQueue.QueueId,
                ToEmails = emailQueue.ToEmails,
                CcEmails = emailQueue.CcEmails,
                BccEmails = emailQueue.BccEmails,
                Subject = emailQueue.Subject,
                Body = emailQueue.Body,
                IsHtml = emailQueue.IsHtml,
                Priority = emailQueue.Priority,
                TemplateId = emailQueue.TemplateId,
                TemplateData = emailQueue.TemplateData,
                RequiresTemplateProcessing = emailQueue.RequiresTemplateProcessing,
                Attachments = emailQueue.Attachments,
                HasEmbeddedImages = emailQueue.HasEmbeddedImages,
                ScheduledFor = emailQueue.ScheduledFor,
                IsScheduled = emailQueue.IsScheduled
            };
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

        /// <summary>
        /// Get current processing statistics
        /// </summary>
        public ProcessingWorkerStats GetStats()
        {
            return new ProcessingWorkerStats
            {
                IsRunning = true,
                CurrentlyProcessing = _processingCount,
                MaxConcurrentWorkers = _processingSettings.MaxConcurrentWorkers,
                AvailableWorkerSlots = Math.Max(0, _processingSettings.MaxConcurrentWorkers - _processingCount),
                LastProcessingTime = _lastProcessingTime,
                UtilizationPercentage = _processingSettings.MaxConcurrentWorkers > 0
                    ? (double)_processingCount / _processingSettings.MaxConcurrentWorkers * 100
                    : 0
            };
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