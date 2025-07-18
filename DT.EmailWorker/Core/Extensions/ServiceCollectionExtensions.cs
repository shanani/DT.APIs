using DT.EmailWorker.Data;
using DT.EmailWorker.Services.Interfaces;
using DT.EmailWorker.Services.Implementations;
using DT.EmailWorker.Repositories.Interfaces;
using DT.EmailWorker.Repositories.Implementations;
using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Core.Utilities;
using DT.EmailWorker.Monitoring.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DT.EmailWorker.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add all Email Worker services
        /// </summary>
        public static IServiceCollection AddEmailWorkerServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.Configure<EmailWorkerSettings>(configuration.GetSection("EmailWorker"));
            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.Configure<ProcessingSettings>(configuration.GetSection("ProcessingSettings"));
            services.Configure<CleanupSettings>(configuration.GetSection("CleanupSettings"));

            // Database
            services.AddDbContext<EmailDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
            services.AddScoped<IEmailHistoryRepository, EmailHistoryRepository>();
            services.AddScoped<ITemplateRepository, TemplateRepository>();
            services.AddScoped<IProcessingLogRepository, ProcessingLogRepository>();

            // Services
            services.AddScoped<IEmailQueueService, EmailQueueService>();
            services.AddScoped<IEmailProcessingService, EmailProcessingService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<ISmtpService, SmtpService>();
            services.AddScoped<ISchedulingService, SchedulingService>();
            services.AddScoped<IHealthService, HealthService>();
            services.AddScoped<ICleanupService, CleanupService>();

            // Utilities
            services.AddScoped<AttachmentProcessor>();

            // Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<EmailDbContext>("database")
                .AddCheck<DatabaseHealthCheck>("database-extended")
                .AddCheck<SmtpHealthCheck>("smtp")
                .AddCheck<QueueHealthCheck>("queue");

            return services;
        }
    }
}