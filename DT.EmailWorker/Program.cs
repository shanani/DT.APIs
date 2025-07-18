using DT.EmailWorker.Core.Extensions;
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

// Add Email Worker services
builder.Services.AddEmailWorkerServices(builder.Configuration);

// Add Background Services (Workers)
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

// Initialize database and seed default templates
using (var scope = host.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<EmailDbContext>();

        // Create database if not exists
        await context.Database.EnsureCreatedAsync();

        // Seed default templates
        await DefaultTemplateSeeder.SeedAsync(context);

        Console.WriteLine("Database initialized and templates seeded successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

// Run the service
await host.RunAsync();