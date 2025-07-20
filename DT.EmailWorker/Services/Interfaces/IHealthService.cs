using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Service for monitoring email worker health and performance
    /// </summary>
    public interface IHealthService
    {
        /// <summary>
        /// Update service status with current health information
        /// </summary>
        /// <param name="status">Current health status</param>
        /// <param name="additionalInfo">Additional status information</param>
        /// <returns>Task</returns>
        Task UpdateServiceStatusAsync(ServiceHealthStatus status, Dictionary<string, object>? additionalInfo = null);

        /// <summary>
        /// Get current service status
        /// </summary>
        /// <returns>Service status DTO</returns>
        Task<ServiceStatusDto> GetServiceStatusAsync();

        /// <summary>
        /// Get service health information (alias for GetServiceStatusAsync for backward compatibility)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service status DTO</returns>
        Task<ServiceStatusDto> GetServiceHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get service status for a specific machine
        /// </summary>
        /// <param name="machineName">Machine name</param>
        /// <returns>Service status DTO or null if not found</returns>
        Task<ServiceStatusDto?> GetServiceStatusByMachineAsync(string machineName);

        /// <summary>
        /// Get all active service instances
        /// </summary>
        /// <returns>List of service status DTOs</returns>
        Task<List<ServiceStatusDto>> GetAllServiceStatusAsync();

        /// <summary>
        /// Perform health check on database connectivity
        /// </summary>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> CheckDatabaseHealthAsync();

        /// <summary>
        /// Perform health check on SMTP connectivity
        /// </summary>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> CheckSmtpHealthAsync();

        /// <summary>
        /// Perform health check on email queue
        /// </summary>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> CheckQueueHealthAsync();

        /// <summary>
        /// Perform comprehensive health check
        /// </summary>
        /// <returns>Overall health check result</returns>
        Task<OverallHealthResult> PerformHealthCheckAsync();

        /// <summary>
        /// Update performance metrics
        /// </summary>
        /// <param name="metrics">Performance metrics</param>
        /// <returns>Task</returns>
        Task UpdatePerformanceMetricsAsync(PerformanceMetrics metrics);

        /// <summary>
        /// Get performance metrics for a time period
        /// </summary>
        /// <param name="hours">Number of hours to look back</param>
        /// <returns>Performance metrics summary</returns>
        Task<PerformanceMetricsSummary> GetPerformanceMetricsAsync(int hours = 24);

        /// <summary>
        /// Log a processing error
        /// </summary>
        /// <param name="queueId">Queue ID if applicable</param>
        /// <param name="error">Error message</param>
        /// <param name="exception">Exception details</param>
        /// <param name="workerId">Worker ID</param>
        /// <returns>Task</returns>
        Task LogProcessingErrorAsync(Guid? queueId, string error, string? exception = null, string? workerId = null);

        /// <summary>
        /// Log processing information
        /// </summary>
        /// <param name="queueId">Queue ID if applicable</param>
        /// <param name="message">Log message</param>
        /// <param name="workerId">Worker ID</param>
        /// <param name="step">Processing step</param>
        /// <returns>Task</returns>
        Task LogProcessingInfoAsync(Guid? queueId, string message, string? workerId = null, string? step = null);

        /// <summary>
        /// Check if any services are unhealthy
        /// </summary>
        /// <param name="heartbeatThresholdMinutes">Minutes after which a service is considered offline</param>
        /// <returns>List of unhealthy services</returns>
        Task<List<ServiceStatusDto>> GetUnhealthyServicesAsync(int heartbeatThresholdMinutes = 10);

        /// <summary>
        /// Calculate system resource usage
        /// </summary>
        /// <returns>Resource usage metrics</returns>
        Task<ResourceUsageMetrics> GetResourceUsageAsync();

        /// <summary>
        /// Send health alert notification
        /// </summary>
        /// <param name="alertLevel">Alert severity level</param>
        /// <param name="message">Alert message</param>
        /// <param name="details">Additional details</param>
        /// <returns>Task</returns>
        Task SendHealthAlertAsync(AlertLevel alertLevel, string message, Dictionary<string, object>? details = null);

        /// <summary>
        /// Clean up old health records
        /// </summary>
        /// <param name="retentionDays">Number of days to retain</param>
        /// <returns>Number of records cleaned up</returns>
        Task<int> CleanupOldHealthRecordsAsync(int retentionDays);
    }

    /// <summary>
    /// Overall health result (custom class for comprehensive health checks)
    /// </summary>
    public class OverallHealthResult
    {
        public ServiceHealthStatus OverallStatus { get; set; }
        public List<HealthCheckResult> ComponentResults { get; set; } = new List<HealthCheckResult>();
        public TimeSpan TotalCheckTime { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow.AddHours(3);
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        public int EmailsProcessedLastHour { get; set; }
        public int EmailsFailedLastHour { get; set; }
        public double AverageProcessingTimeMs { get; set; }
        public int CurrentQueueDepth { get; set; }
        public int ActiveWorkers { get; set; }
        public decimal CpuUsagePercent { get; set; }
        public decimal MemoryUsageMB { get; set; }
        public decimal DiskUsagePercent { get; set; }
    }

    /// <summary>
    /// Performance metrics summary
    /// </summary>
    public class PerformanceMetricsSummary
    {
        public int TotalEmailsProcessed { get; set; }
        public int TotalEmailsFailed { get; set; }
        public double SuccessRate { get; set; }
        public double AverageProcessingTimeMs { get; set; }
        public double MaxProcessingTimeMs { get; set; }
        public double MinProcessingTimeMs { get; set; }
        public int PeakQueueDepth { get; set; }
        public int AverageQueueDepth { get; set; }
        public double AverageEmailsPerHour { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    /// <summary>
    /// Resource usage metrics
    /// </summary>
    public class ResourceUsageMetrics
    {
        public decimal CpuUsagePercent { get; set; }
        public decimal MemoryUsageMB { get; set; }
        public decimal MemoryUsagePercent { get; set; }
        public decimal DiskUsagePercent { get; set; }
        public long AvailableMemoryMB { get; set; }
        public long TotalMemoryMB { get; set; }
        public int ProcessCount { get; set; }
        public int ThreadCount { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// Alert severity levels
    /// </summary>
    public enum AlertLevel
    {
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}