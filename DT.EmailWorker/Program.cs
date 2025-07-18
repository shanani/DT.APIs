using DT.EmailWorker.Data;
using DT.EmailWorker.Services.Interfaces;
using DT.EmailWorker.Services.Implementations;
using DT.EmailWorker.Workers;
using DT.EmailWorker.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

// Configure Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DT.EmailWorker";
});

// Database Configuration
builder.Services.AddDbContext<EmailDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EmailDbConn"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});

// Core Services
builder.Services.AddScoped<IEmailProcessingService, EmailProcessingService>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<ICleanupService, CleanupService>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();

// Background Workers
builder.Services.AddHostedService<EmailProcessingWorker>();
builder.Services.AddHostedService<ScheduledEmailWorker>();
builder.Services.AddHostedService<HealthCheckWorker>();
builder.Services.AddHostedService<CleanupWorker>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EmailDbContext>("database")
    .AddCheck("email_queue", () => HealthCheckResult.Healthy("Queue is accessible"))
    .AddCheck("smtp_connection", () => HealthCheckResult.Healthy("SMTP server is reachable"));

// Configuration
builder.Services.Configure<EmailWorkerSettings>(
    builder.Configuration.GetSection("EmailWorkerSettings"));
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

var host = builder.Build();

// Ensure database is created and migrated
using (var scope = host.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<EmailDbContext>();

        // Create database if it doesn't exist
        await context.Database.EnsureCreatedAsync();

        // Run any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("DT.EmailWorker database initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize database");
        throw;
    }
}

// Start the service
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("DT.EmailWorker starting...");

await host.RunAsync();

logger.LogInformation("DT.EmailWorker stopped.");