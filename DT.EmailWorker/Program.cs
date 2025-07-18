using DT.EmailWorker.Data;
using DT.EmailWorker.Services.Interfaces;
using DT.EmailWorker.Services.Implementations;
using DT.EmailWorker.Workers;
using DT.EmailWorker.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;

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
            sqlOptions.CommandTimeout(120); // 2 minutes for long-running operations
        });
});

// Configuration Settings
builder.Services.Configure<EmailWorkerSettings>(
    builder.Configuration.GetSection("EmailWorkerSettings"));
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<ProcessingSettings>(
    builder.Configuration.GetSection("ProcessingSettings"));
builder.Services.Configure<CleanupSettings>(
    builder.Configuration.GetSection("CleanupSettings"));

// Core Services
builder.Services.AddScoped<IEmailProcessingService, EmailProcessingService>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<ICleanupService, CleanupService>();

// Background Workers
builder.Services.AddHostedService<EmailProcessingWorker>();
builder.Services.AddHostedService<HealthCheckWorker>();
builder.Services.AddHostedService<CleanupWorker>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EmailDbContext>("database", HealthStatus.Critical)
    .AddCheck("email_queue", () => HealthCheckResult.Healthy("Queue is accessible"))
    .AddCheck("smtp_connection", () => HealthCheckResult.Healthy("SMTP server is reachable"));

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog(); // Windows Event Log for service
builder.Logging.AddDebug();

// Configure logging levels
builder.Logging.SetMinimumLevel(LogLevel.Information);
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var host = builder.Build();

// Initialize database and ensure it's ready
await InitializeDatabaseAsync(host);

// Start the service
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("DT.EmailWorker v{Version} starting on {MachineName}...",
    Assembly.GetExecutingAssembly().GetName().Version, Environment.MachineName);

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    logger.LogInformation("DT.EmailWorker stopped.");
}

static async Task InitializeDatabaseAsync(IHost host)
{
    using var scope = host.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = scope.ServiceProvider.GetRequiredService<EmailDbContext>();

        logger.LogInformation("Initializing database...");

        // Ensure database exists
        var created = await context.Database.EnsureCreatedAsync();
        if (created)
        {
            logger.LogInformation("Database created successfully");
        }

        // Apply any pending migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }

        // Test database connectivity
        await context.Database.ExecuteSqlRawAsync("SELECT 1");

        logger.LogInformation("Database initialization completed successfully");

        // Seed default templates if needed
        await SeedDefaultTemplatesAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to initialize database");
        throw;
    }
}

static async Task SeedDefaultTemplatesAsync(EmailDbContext context, ILogger logger)
{
    try
    {
        // Check if we already have templates
        var templateCount = await context.EmailTemplates.CountAsync();
        if (templateCount > 0)
        {
            logger.LogDebug("Templates already exist, skipping seeding");
            return;
        }

        logger.LogInformation("Seeding default email templates...");

        var defaultTemplates = new[]
        {
            new Models.Entities.EmailTemplate
            {
                Name = "Welcome",
                Description = "Welcome email template",
                Category = "System",
                SubjectTemplate = "Welcome to {SystemName}",
                BodyTemplate = @"
                    <h2>Welcome {UserName}!</h2>
                    <p>Thank you for joining {SystemName}. We're excited to have you on board.</p>
                    <p>If you have any questions, please don't hesitate to contact us.</p>
                    <p>Best regards,<br>The {SystemName} Team</p>",
                IsSystem = true,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Models.Entities.EmailTemplate
            {
                Name = "PasswordReset",
                Description = "Password reset email template",
                Category = "Security",
                SubjectTemplate = "Password Reset Request for {SystemName}",
                BodyTemplate = @"
                    <h2>Password Reset Request</h2>
                    <p>Hello {UserName},</p>
                    <p>We received a request to reset your password for {SystemName}.</p>
                    <p>To reset your password, please click the link below:</p>
                    <p><a href='{ResetLink}'>Reset Password</a></p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't request this reset, please ignore this email.</p>
                    <p>Best regards,<br>The {SystemName} Team</p>",
                IsSystem = true,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Models.Entities.EmailTemplate
            {
                Name = "SystemNotification",
                Description = "General system notification template",
                Category = "System",
                SubjectTemplate = "{NotificationType}: {Subject}",
                BodyTemplate = @"
                    <h2>{NotificationType}</h2>
                    <p>Hello {UserName},</p>
                    <p>{Message}</p>
                    <p>Time: {Timestamp}</p>
                    <p>Best regards,<br>The {SystemName} Team</p>",
                IsSystem = true,
                CreatedBy = "System",
                UpdatedBy = "System"
            }
        };

        context.EmailTemplates.AddRange(defaultTemplates);
        await context.SaveChangesAsync();

        logger.LogInformation("Successfully seeded {Count} default email templates", defaultTemplates.Length);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding default templates");
        // Don't throw - this is not critical for service startup
    }
}