// ============================================================================
// COMPLETE FIX: ServiceCollectionExtensions.cs
// ============================================================================

using DT.EmailWorker.Data;
using DT.EmailWorker.Services.Interfaces;
using DT.EmailWorker.Services.Implementations;
using DT.EmailWorker.Repositories.Interfaces;
using DT.EmailWorker.Repositories.Implementations;
using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Core.Utilities;
using DT.EmailWorker.Core.Engines;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            // 🚨 CRITICAL: Verify connection string first
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string 'DefaultConnection' not found in configuration!");
            }

            // 🚀 FIXED: Configuration section names to match your appsettings.json
            services.Configure<EmailWorkerSettings>(configuration.GetSection("EmailWorker"));
            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.Configure<ProcessingSettings>(configuration.GetSection("ProcessingSettings"));
            services.Configure<CleanupSettings>(configuration.GetSection("CleanupSettings"));

            // Database
            services.AddDbContext<EmailDbContext>(options =>
                options.UseSqlServer(connectionString));

            // 🚀 Register CidImageProcessor
            services.AddScoped<CidImageProcessor>();

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
            services.AddScoped<ICleanupService, CleanupService>();

            // 🚀 CRITICAL FIX: Register IHealthService (was missing!)
            services.AddScoped<IHealthService, HealthService>();

            // Utilities
            services.AddScoped<AttachmentProcessor>();

            // Basic Health Checks (simplified - no custom health check classes needed)
            services.AddHealthChecks()
                .AddDbContextCheck<EmailDbContext>("database");

            return services;
        }
    }
}

 