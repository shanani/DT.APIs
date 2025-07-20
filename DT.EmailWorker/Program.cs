// ============================================================================
// FIXED Program.cs - Matches Your Exact Configuration
// ============================================================================

using DT.EmailWorker.Core.Extensions;
using DT.EmailWorker.Core.Engines;
using DT.EmailWorker.Workers;
using DT.EmailWorker.Data;
using DT.EmailWorker.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Add NLog
builder.Logging.ClearProviders();
builder.Logging.AddNLog();

// 🚨 FIXED: Using your existing ServiceCollectionExtensions + CidImageProcessor
builder.Services.AddEmailWorkerServices(builder.Configuration);

// 🚀 ADD: CidImageProcessor registration (was missing!)
builder.Services.AddScoped<CidImageProcessor>();

// Add Background Services (Workers) - Your existing setup
builder.Services.AddHostedService<EmailProcessingWorker>();
builder.Services.AddHostedService<ScheduledEmailWorker>();
builder.Services.AddHostedService<HealthCheckWorker>();
builder.Services.AddHostedService<CleanupWorker>();
builder.Services.AddHostedService<StatusReportWorker>();

// Configure as Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DT.EmailWorker";
});

var host = builder.Build();

// 🚀 ENHANCED: Database initialization with better logging
using (var scope = host.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Test connection string
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        logger.LogInformation("✅ Using connection string: {ConnectionString}",
            connectionString?.Substring(0, Math.Min(50, connectionString.Length)) + "...");

        // Test database connection
        logger.LogInformation("🔍 Testing database connection...");
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new InvalidOperationException("Cannot connect to database!");
        }
        logger.LogInformation("✅ Database connection successful!");

        // Create database if not exists
        logger.LogInformation("🔍 Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("✅ Database migrations completed!");

        // Seed default templates
        logger.LogInformation("🔍 Seeding default templates...");
        await DefaultTemplateSeeder.SeedAsync(context);
        logger.LogInformation("✅ Templates seeded successfully!");

        // Verify tables exist
        var queueCount = await context.EmailQueue.CountAsync();
        var templateCount = await context.EmailTemplates.CountAsync();
        logger.LogInformation("✅ Database ready! Queue: {QueueCount}, Templates: {TemplateCount}",
            queueCount, templateCount);

        Console.WriteLine("✅ Database initialized and templates seeded successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Database initialization failed!");
        Console.WriteLine($"❌ Database error: {ex.Message}");
        throw;
    }
}

Console.WriteLine("🚀 DT.EmailWorker starting...");

// Run the service
await host.RunAsync();