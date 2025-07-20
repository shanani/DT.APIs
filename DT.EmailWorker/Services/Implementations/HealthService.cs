using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Data;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// Implementation of health monitoring service
    /// </summary>
    public class HealthService : IHealthService
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<HealthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _serviceName;
        private readonly string _serviceVersion;
        private readonly ProcessingSettings _processingSettings;

        public HealthService(
            EmailDbContext context,
            ILogger<HealthService> logger,
            IOptions<ProcessingSettings> processingSettings,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _processingSettings = processingSettings.Value;    // ← ADD THIS LINE
            _serviceName = "DT.EmailWorker";
            _serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }


        // UPDATE UpdateServiceStatusAsync METHOD TO INCLUDE:
        public async Task UpdateServiceStatusAsync(ServiceHealthStatus status, Dictionary<string, object>? additionalInfo = null)
        {
            try
            {
                var machineName = Environment.MachineName;
                var serviceStatus = await _context.ServiceStatus
                    .FirstOrDefaultAsync(s => s.ServiceName == _serviceName && s.MachineName == machineName);

                if (serviceStatus == null)
                {
                    serviceStatus = new ServiceStatus
                    {
                        ServiceName = _serviceName,
                        MachineName = machineName,
                        ServiceVersion = _serviceVersion,
                        StartedAt = DateTime.UtcNow.AddHours(3),
                        // ← ADD THESE LINES:
                        MaxConcurrentWorkers = _processingSettings.MaxConcurrentWorkers,
                        BatchSize = _processingSettings.BatchSize
                    };
                    _context.ServiceStatus.Add(serviceStatus);
                }

                serviceStatus.Status = status;
                serviceStatus.LastHeartbeat = DateTime.UtcNow.AddHours(3);
                serviceStatus.UpdatedAt = DateTime.UtcNow.AddHours(3);

                // ← ADD THESE LINES TO UPDATE CONFIG VALUES:
                serviceStatus.MaxConcurrentWorkers = _processingSettings.MaxConcurrentWorkers;
                serviceStatus.BatchSize = _processingSettings.BatchSize;

                // Update performance metrics
                var metrics = await GetResourceUsageAsync();
                serviceStatus.CpuUsagePercent = metrics.CpuUsagePercent;
                serviceStatus.MemoryUsageMB = metrics.MemoryUsageMB;
                serviceStatus.DiskUsagePercent = metrics.DiskUsagePercent;

                // Store additional info as JSON
                if (additionalInfo != null && additionalInfo.ContainsKey("error"))
                {
                    serviceStatus.LastError = additionalInfo["error"]?.ToString();
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service status");
                throw;
            }
        }



        /// <summary>
        /// Get service health information (alias for GetServiceStatusAsync for backward compatibility)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service status DTO</returns>
        public async Task<ServiceStatusDto> GetServiceHealthAsync(CancellationToken cancellationToken = default)
        {
            // This is just an alias that calls the existing GetServiceStatusAsync method
            return await GetServiceStatusAsync();
        }

        public async Task<ServiceStatusDto> GetServiceStatusAsync()
        {
            try
            {
                var machineName = Environment.MachineName;
                var serviceStatus = await _context.ServiceStatus
                    .FirstOrDefaultAsync(s => s.ServiceName == _serviceName && s.MachineName == machineName);

                return serviceStatus != null ? MapToServiceStatusDto(serviceStatus) : new ServiceStatusDto
                {
                    ServiceName = _serviceName,
                    MachineName = machineName,
                    Status = ServiceHealthStatus.Unknown,
                    ServiceVersion = _serviceVersion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status");
                throw;
            }
        }

        public async Task<ServiceStatusDto?> GetServiceStatusByMachineAsync(string machineName)
        {
            try
            {
                var serviceStatus = await _context.ServiceStatus
                    .FirstOrDefaultAsync(s => s.ServiceName == _serviceName && s.MachineName == machineName);

                return serviceStatus != null ? MapToServiceStatusDto(serviceStatus) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status for machine {MachineName}", machineName);
                throw;
            }
        }

        public async Task<List<ServiceStatusDto>> GetAllServiceStatusAsync()
        {
            try
            {
                var serviceStatuses = await _context.ServiceStatus
                    .Where(s => s.ServiceName == _serviceName)
                    .OrderBy(s => s.MachineName)
                    .ToListAsync();

                return serviceStatuses.Select(MapToServiceStatusDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all service statuses");
                throw;
            }
        }

        public async Task<HealthCheckResult> CheckDatabaseHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Test database connectivity with a simple query
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");

                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["ConnectionString"] = _context.Database.GetConnectionString()?.Split(';').FirstOrDefault() ?? "Unknown",
                    ["Provider"] = _context.Database.ProviderName ?? "Unknown"
                };

                return HealthCheckResult.Healthy("Database is accessible", data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["Error"] = ex.Message
                };

                return HealthCheckResult.Unhealthy("Database connection failed", ex, data);
            }
        }

        public async Task<HealthCheckResult> CheckSmtpHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var server = smtpSettings["Server"];
                var port = int.Parse(smtpSettings["Port"] ?? "25");

                using var client = new SmtpClient();
                await client.ConnectAsync(server, port, SecureSocketOptions.None);

                if (!string.IsNullOrEmpty(smtpSettings["Username"]))
                {
                    await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
                }

                await client.DisconnectAsync(true);
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["Server"] = server ?? "Unknown",
                    ["Port"] = port,
                    ["AuthenticationRequired"] = !string.IsNullOrEmpty(smtpSettings["Username"])
                };

                return HealthCheckResult.Healthy("SMTP server is accessible", data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["Error"] = ex.Message
                };

                return HealthCheckResult.Unhealthy("SMTP server connection failed", ex, data);
            }
        }

        public async Task<HealthCheckResult> CheckQueueHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queueCount = await _context.EmailQueue.CountAsync();
                var processingCount = await _context.EmailQueue
                    .CountAsync(e => e.Status == EmailQueueStatus.Processing);

                stopwatch.Stop();

                var isHealthy = queueCount < 10000; // Alert if queue gets too large

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["TotalQueueCount"] = queueCount,
                    ["ProcessingCount"] = processingCount,
                    ["QueueHealthy"] = isHealthy
                };

                var message = isHealthy ? "Queue is healthy" : "Queue depth is high";

                return isHealthy
                    ? HealthCheckResult.Healthy(message, data)
                    : HealthCheckResult.Degraded(message, data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["Error"] = ex.Message
                };

                return HealthCheckResult.Unhealthy("Queue health check failed", ex, data);
            }
        }

        public async Task<OverallHealthResult> PerformHealthCheckAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new OverallHealthResult();

            try
            {
                // Perform all health checks
                var databaseCheck = await CheckDatabaseHealthAsync();
                var smtpCheck = await CheckSmtpHealthAsync();
                var queueCheck = await CheckQueueHealthAsync();

                result.ComponentResults.AddRange(new[] { databaseCheck, smtpCheck, queueCheck });

                // Determine overall status based on Microsoft's HealthStatus enum
                if (result.ComponentResults.All(r => r.Status == HealthStatus.Healthy))
                {
                    result.OverallStatus = ServiceHealthStatus.Healthy;
                    result.Summary = "All components are healthy";
                }
                else if (result.ComponentResults.Any(r => r.Status == HealthStatus.Unhealthy &&
                    r.Description?.Contains("Database", StringComparison.OrdinalIgnoreCase) == true))
                {
                    result.OverallStatus = ServiceHealthStatus.Critical;
                    result.Summary = "Critical: Database is unavailable";
                }
                else if (result.ComponentResults.Count(r => r.Status == HealthStatus.Unhealthy) == 1)
                {
                    result.OverallStatus = ServiceHealthStatus.Warning;
                    result.Summary = "Warning: Some components are unhealthy";
                }
                else
                {
                    result.OverallStatus = ServiceHealthStatus.Critical;
                    result.Summary = "Critical: Multiple components are unhealthy";
                }

                stopwatch.Stop();
                result.TotalCheckTime = stopwatch.Elapsed;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error performing health check");

                result.OverallStatus = ServiceHealthStatus.Critical;
                result.Summary = "Health check system failure";
                result.TotalCheckTime = stopwatch.Elapsed;

                return result;
            }
        }

        public async Task UpdatePerformanceMetricsAsync(PerformanceMetrics metrics)
        {
            try
            {
                var machineName = Environment.MachineName;
                var serviceStatus = await _context.ServiceStatus
                    .FirstOrDefaultAsync(s => s.ServiceName == _serviceName && s.MachineName == machineName);

                if (serviceStatus != null)
                {
                    serviceStatus.CpuUsagePercent = metrics.CpuUsagePercent;
                    serviceStatus.MemoryUsageMB = metrics.MemoryUsageMB;
                    serviceStatus.DiskUsagePercent = metrics.DiskUsagePercent;
                    serviceStatus.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    await _context.SaveChangesAsync();
                }

                _logger.LogDebug("Performance metrics updated: CPU {CPU}%, Memory {Memory}MB, Queue {Queue}",
                    metrics.CpuUsagePercent, metrics.MemoryUsageMB, metrics.CurrentQueueDepth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating performance metrics");
                throw;
            }
        }

        public async Task<PerformanceMetricsSummary> GetPerformanceMetricsAsync(int hours = 24)
        {
            try
            {
                var startTime = DateTime.UtcNow.AddHours(3).AddHours(-hours);
                var endTime = DateTime.UtcNow.AddHours(3);

                var processedEmails = await _context.EmailHistory
                    .Where(h => h.SentAt >= startTime && h.SentAt <= endTime)
                    .CountAsync();

                var failedEmails = await _context.ProcessingLogs
                    .Where(l => l.LogLevel == LogLevel.Error && l.CreatedAt >= startTime && l.CreatedAt <= endTime)
                    .CountAsync();

                var successRate = processedEmails > 0 ? ((double)(processedEmails - failedEmails) / processedEmails) * 100 : 100;

                return new PerformanceMetricsSummary
                {
                    TotalEmailsProcessed = processedEmails,
                    TotalEmailsFailed = failedEmails,
                    SuccessRate = successRate,
                    AverageEmailsPerHour = hours > 0 ? (double)processedEmails / hours : 0,
                    PeriodStart = startTime,
                    PeriodEnd = endTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                throw;
            }
        }

        public async Task LogProcessingErrorAsync(Guid? queueId, string error, string? exception = null, string? workerId = null)
        {
            try
            {
                var log = new ProcessingLog
                {
                    LogLevel = LogLevel.Error,
                    Category = "EmailProcessing",
                    Message = error,
                    Exception = exception,
                    QueueId = queueId,
                    WorkerId = workerId
                };

                _context.ProcessingLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging processing error");
                // Don't rethrow to avoid cascading failures
            }
        }

        public async Task LogProcessingInfoAsync(Guid? queueId, string message, string? workerId = null, string? step = null)
        {
            try
            {
                var log = new ProcessingLog
                {
                    LogLevel = LogLevel.Information,
                    Category = "EmailProcessing",
                    Message = message,
                    QueueId = queueId,
                    WorkerId = workerId,
                    ProcessingStep = step
                };

                _context.ProcessingLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging processing info");
                // Don't rethrow to avoid cascading failures
            }
        }

        public async Task<List<ServiceStatusDto>> GetUnhealthyServicesAsync(int heartbeatThresholdMinutes = 10)
        {
            try
            {
                var thresholdTime = DateTime.UtcNow.AddHours(3).AddMinutes(-heartbeatThresholdMinutes);

                var unhealthyServices = await _context.ServiceStatus
                    .Where(s => s.Status != ServiceHealthStatus.Healthy || s.LastHeartbeat < thresholdTime)
                    .ToListAsync();

                return unhealthyServices.Select(MapToServiceStatusDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unhealthy services");
                throw;
            }
        }

        public async Task<ResourceUsageMetrics> GetResourceUsageAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var totalMemory = GC.GetTotalMemory(false);

                return new ResourceUsageMetrics
                {
                    MemoryUsageMB = totalMemory / 1024 / 1024,
                    ProcessCount = Process.GetProcesses().Length,
                    ThreadCount = process.Threads.Count,
                    Uptime = DateTime.UtcNow.AddHours(3) - process.StartTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resource usage");
                throw;
            }
        }

        public async Task SendHealthAlertAsync(AlertLevel alertLevel, string message, Dictionary<string, object>? details = null)
        {
            try
            {
                // Log the alert
                _logger.LogWarning("Health Alert [{Level}]: {Message} - Details: {Details}",
                    alertLevel, message, details != null ? System.Text.Json.JsonSerializer.Serialize(details) : "None");

                // Here you would implement actual alerting (email, SMS, webhook, etc.)
                // For now, we'll just log it

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending health alert");
                // Don't rethrow to avoid cascading failures
            }
        }

        public async Task<int> CleanupOldHealthRecordsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddHours(3).AddDays(-retentionDays);

                var recordsToDelete = await _context.ProcessingLogs
                    .Where(l => l.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (recordsToDelete.Any())
                {
                    _context.ProcessingLogs.RemoveRange(recordsToDelete);
                    await _context.SaveChangesAsync();
                }

                return recordsToDelete.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old health records");
                throw;
            }
        }

        private ServiceStatusDto MapToServiceStatusDto(ServiceStatus serviceStatus)
        {
            return new ServiceStatusDto
            {
                ServiceName = serviceStatus.ServiceName,
                MachineName = serviceStatus.MachineName,
                Status = serviceStatus.Status,
                ServiceVersion = serviceStatus.ServiceVersion,
                StartedAt = serviceStatus.StartedAt,
                LastHeartbeat = serviceStatus.LastHeartbeat,
                UpdatedAt = serviceStatus.UpdatedAt,
                CpuUsagePercent = serviceStatus.CpuUsagePercent,
                MemoryUsageMB = serviceStatus.MemoryUsageMB,
                DiskUsagePercent = serviceStatus.DiskUsagePercent,
                AdditionalInfo = serviceStatus.AdditionalInfo
            };
        }
    }
}