using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DT.EmailWorker.Workers
{
    /// <summary>
    /// Background worker for processing scheduled emails
    /// </summary>
    public class ScheduledEmailWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailWorkerSettings _settings;
        private readonly ILogger<ScheduledEmailWorker> _logger;

        public ScheduledEmailWorker(
            IServiceProvider serviceProvider,
            IOptions<EmailWorkerSettings> settings,
            ILogger<ScheduledEmailWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Email Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledEmailsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing scheduled emails");
                }

                // Wait for the configured interval (default 1 minute)
                var delay = TimeSpan.FromMinutes(_settings.ScheduledEmailCheckIntervalMinutes);
                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("Scheduled Email Worker stopped");
        }

        private async Task ProcessScheduledEmailsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var schedulingService = scope.ServiceProvider.GetRequiredService<ISchedulingService>();

            try
            {
                var processedCount = await schedulingService.ProcessDueEmailsAsync(cancellationToken);

                if (processedCount > 0)
                {
                    _logger.LogInformation("Processed {Count} scheduled emails", processedCount);
                }
                else
                {
                    _logger.LogDebug("No scheduled emails due for processing");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process scheduled emails");
            }
        }
    }
}