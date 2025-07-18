using DT.EmailWorker.Data;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using MailKit.Net.Smtp;
using MailKit.Security;

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

        public HealthService(
            EmailDbContext context,
            ILogger<HealthService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _serviceName = "DT.EmailWorker";
            _serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

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
                        StartedAt = DateTime.UtcNow
                    };
                    _context.ServiceStatus.Add(serviceStatus);
                }

                serviceStatus.Status = status;
                serviceStatus.LastHeartbeat = DateTime.UtcNow;
                serviceStatus.UpdatedAt = DateTime.UtcNow;

                // Update performance metrics
                var metrics = await GetResourceUsageAsync();
                serviceStatus.CpuUsagePercent = metrics.CpuUsagePercent;
                serviceStatus.MemoryUsageMB = metrics.MemoryUsageMB;
                serviceStatus.UptimeSeconds = (long)metrics.Uptime.TotalSeconds;

                // Get queue statistics
                var queueStats = await GetQueueStatisticsAsync();
                serviceStatus.QueueDepth = queueStats.TotalQueued;

                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated service status to {Status}", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service status");
                throw;
            }
        }

        public async Task<ServiceStatusDto> GetServiceStatusAsync()
        {
            try
            {
                var machineName = Environment.MachineName;
                var serviceStatus = await _context.ServiceStatus
                    .FirstOrDefaultAsync(s => s.ServiceName == _serviceName && s.MachineName == machineName);

                if (serviceStatus == null)
                {
                    return new ServiceStatusDto
                    {
                        ServiceName = _serviceName,
                        MachineName = machineName,
                        Status = ServiceHealthStatus.Offline,
                        StatusDescription = "Service not found in database"
                    };
                }

                return MapToServiceStatusDto(serviceStatus);
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

                return new HealthCheckResult
                {
                    IsHealthy = true,
                    ComponentName = "Database",
                    Message = "Database is accessible",
                    ResponseTime = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["ConnectionString"] = _context.Database.GetConnectionString()?.Split(';').FirstOrDefault() ?? "Unknown",
                        ["Provider"] = _context.Database.ProviderName ?? "Unknown"
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    ComponentName = "Database",
                    Message = "Database connection failed",
                    ResponseTime = stopwatch.Elapsed,
                    Exception = ex
                };
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

                return new HealthCheckResult
                {
                    IsHealthy = true,
                    ComponentName = "SMTP",
                    Message = "SMTP server is accessible",
                    ResponseTime = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["Server"] = server ?? "Unknown",
                        ["Port"] = port,
                        ["AuthenticationRequired"] = !string.IsNullOrEmpty(smtpSettings["Username"])
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    ComponentName = "SMTP",
                    Message = "SMTP server connection failed",
                    ResponseTime = stopwatch.Elapsed,
                    Exception = ex
                };
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

                return new HealthCheckResult
                {
                    IsHealthy = isHealthy,
                    ComponentName = "Queue",
                    Message = isHealthy ? "Queue is healthy" : "Queue depth is high",
                    ResponseTime = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["TotalQueueCount"] = queueCount,
                        ["ProcessingCount"] = processingCount,
                        ["QueueHealthy"] = isHealthy
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    ComponentName = "Queue",
                    Message = "Queue health check failed",
                    ResponseTime = stopwatch.Elapsed,
                    Exception = ex
                };
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

                // Determine overall status
                if (result.ComponentResults.All(r => r.IsHealthy))
                {
                    result.OverallStatus = ServiceHealthStatus.Healthy;
                    result.Summary = "All components are healthy";
                }
                else if (result.ComponentResults.Any(r => !r.IsHealthy && r.ComponentName == "Database"))
                {
                    result.OverallStatus = ServiceHealthStatus.Critical;
                    result.Summary = "Critical: Database is unavailable";
                }
                else if (result.ComponentResults.Count(r => !r.IsHealthy) == 1)
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

                return new OverallHealthResult
                {
                    OverallStatus = ServiceHealthStatus.Critical,
                    Summary = "Health check failed with exception",
                    TotalCheckTime = stopwatch.Elapsed,
                    ComponentResults = new List<HealthCheckResult>
                    {
                        new HealthCheckResult
                        {
                            IsHealthy = false,
                            ComponentName = "HealthCheck",
                            Message = "Health check process failed",
                            Exception = ex
                        }
                    }
                };
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
                    serviceStatus.EmailsProcessedPerHour = metrics.EmailsProcessedLastHour;
                    serviceStatus.ErrorRate = metrics.EmailsFailedLastHour > 0 && metrics.EmailsProcessedLastHour > 0
                        ? (decimal)metrics.EmailsFailedLastHour / metrics.EmailsProcessedLastHour * 100
                        : 0;
                    serviceStatus.AverageProcessingTimeMs = (decimal)metrics.AverageProcessingTimeMs;
                    serviceStatus.QueueDepth = metrics.CurrentQueueDepth;
                    serviceStatus.CurrentActiveWorkers = metrics.ActiveWorkers;
                    serviceStatus.CpuUsagePercent = metrics.CpuUsagePercent;
                    serviceStatus.MemoryUsageMB = metrics.MemoryUsageMB;
                    serviceStatus.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
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
                var startTime = DateTime.UtcNow.AddHours(-hours);

                var emailHistory = await _context.EmailHistory
                    .Where(h => h.CreatedAt >= startTime)
                    .ToListAsync();

                var totalProcessed = emailHistory.Count;
                var totalFailed = emailHistory.Count(h => h.Status == EmailQueueStatus.Failed);
                var successRate = totalProcessed > 0 ? (double)(totalProcessed - totalFailed) / totalProcessed * 100 : 100;

                var processingTimes = emailHistory
                    .Where(h => h.ProcessingTimeMs.HasValue)
                    .Select(h => h.ProcessingTimeMs.Value)
                    .ToList();

                return new PerformanceMetricsSummary
                {
                    TotalEmailsProcessed = totalProcessed,
                    TotalEmailsFailed = totalFailed,
                    SuccessRate = successRate,
                    AverageProcessingTimeMs = processingTimes.Any() ? processingTimes.Average() : 0,
                    MaxProcessingTimeMs = processingTimes.Any() ? processingTimes.Max() : 0,
                    MinProcessingTimeMs = processingTimes.Any() ? processingTimes.Min() : 0,
                    AverageEmailsPerHour = totalProcessed > 0 ? (double)totalProcessed / hours : 0,
                    PeriodStart = startTime,
                    PeriodEnd = DateTime.UtcNow
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
                    WorkerId = workerId,
                    ProcessingStep = "Error"
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
                var thresholdTime = DateTime.UtcNow.AddMinutes(-heartbeatThresholdMinutes);

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
                    Uptime = DateTime.UtcNow - process.StartTime
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
                var logLevel = alertLevel switch
                {
                    AlertLevel.Critical => LogLevel.Critical,
                    AlertLevel.Error => LogLevel.Error,
                    AlertLevel.Warning => LogLevel.Warning,
                    _ => LogLevel.Information
                };

                _logger.Log(logLevel, "Health Alert [{AlertLevel}]: {Message}", alertLevel, message);

                // Could implement email/SMS notifications here
                // For now, just log to processing logs
                await LogProcessingErrorAsync(null, $"Health Alert [{alertLevel}]: {message}",
                    details != null ? System.Text.Json.JsonSerializer.Serialize(details) : null);
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
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var oldRecords = await _context.ServiceStatus
                    .Where(s => s.UpdatedAt < cutoffDate)
                    .ToListAsync();

                _context.ServiceStatus.RemoveRange(oldRecords);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old health records", oldRecords.Count);

                return oldRecords.Count;
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
                StatusDescription = GetStatusDescription(serviceStatus.Status),
                LastHeartbeat = serviceStatus.LastHeartbeat,
                QueueDepth = serviceStatus.QueueDepth,
                EmailsProcessedPerHour = serviceStatus.EmailsProcessedPerHour,
                ErrorRate = serviceStatus.ErrorRate,
                AverageProcessingTimeMs = serviceStatus.AverageProcessingTimeMs,
                CpuUsagePercent = serviceStatus.CpuUsagePercent,
                MemoryUsageMB = serviceStatus.MemoryUsageMB,
                MaxConcurrentWorkers = serviceStatus.MaxConcurrentWorkers,
                CurrentActiveWorkers = serviceStatus.CurrentActiveWorkers,
                ServiceVersion = serviceStatus.ServiceVersion,
                Uptime = TimeSpan.FromSeconds(serviceStatus.UptimeSeconds),
                TotalEmailsProcessed = serviceStatus.TotalEmailsProcessed,
                TotalEmailsFailed = serviceStatus.TotalEmailsFailed,
                LastError = serviceStatus.LastError,
                LastErrorAt = serviceStatus.LastErrorAt
            };
        }

        private static string GetStatusDescription(ServiceHealthStatus status)
        {
            return status switch
            {
                ServiceHealthStatus.Healthy => "Service is operating normally",
                ServiceHealthStatus.Warning => "Service has minor issues but is operational",
                ServiceHealthStatus.Critical => "Service has critical issues affecting functionality",
                ServiceHealthStatus.Offline => "Service is offline or unreachable",
                _ => "Unknown status"
            };
        }

        private async Task<QueueStatistics> GetQueueStatisticsAsync()
        {
            var queuedCount = await _context.EmailQueue.CountAsync(e => e.Status == EmailQueueStatus.Queued);
            return new QueueStatistics { TotalQueued = queuedCount };
        }
    }

    
}