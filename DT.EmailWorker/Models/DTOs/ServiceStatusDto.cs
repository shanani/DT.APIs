using DT.EmailWorker.Models.Enums;

namespace DT.EmailWorker.Models.DTOs
{
    /// <summary>
    /// DTO for service status information
    /// </summary>
    public class ServiceStatusDto
    {
        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Machine name where the service is running
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// Current health status
        /// </summary>
        public ServiceHealthStatus Status { get; set; }

        /// <summary>
        /// Status description
        /// </summary>
        public string StatusDescription { get; set; } = string.Empty;

        /// <summary>
        /// Last heartbeat timestamp
        /// </summary>
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// Current queue depth (number of pending emails)
        /// </summary>
        public int QueueDepth { get; set; }

        /// <summary>
        /// Emails processed per hour
        /// </summary>
        public int EmailsProcessedPerHour { get; set; }

        /// <summary>
        /// Error rate percentage
        /// </summary>
        public decimal ErrorRate { get; set; }

        /// <summary>
        /// Average processing time in milliseconds
        /// </summary>
        public decimal? AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public decimal? CpuUsagePercent { get; set; }

        /// <summary>
        /// Memory usage in MB
        /// </summary>
        public decimal? MemoryUsageMB { get; set; }

        /// <summary>
        /// Maximum concurrent workers configured
        /// </summary>
        public int MaxConcurrentWorkers { get; set; }

        /// <summary>
        /// Current number of active workers
        /// </summary>
        public int CurrentActiveWorkers { get; set; }

        /// <summary>
        /// Service version
        /// </summary>
        public string ServiceVersion { get; set; } = string.Empty;

        /// <summary>
        /// Service uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Total emails processed since service start
        /// </summary>
        public long TotalEmailsProcessed { get; set; }

        /// <summary>
        /// Total emails failed since service start
        /// </summary>
        public long TotalEmailsFailed { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public decimal SuccessRate => TotalEmailsProcessed > 0
            ? Math.Round((decimal)(TotalEmailsProcessed - TotalEmailsFailed) / TotalEmailsProcessed * 100, 2)
            : 100;

        /// <summary>
        /// Last error message if any
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// When the last error occurred
        /// </summary>
        public DateTime? LastErrorAt { get; set; }

        /// <summary>
        /// Additional status information
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();
        public DateTime StartedAt { get; internal set; }
        public DateTime UpdatedAt { get; internal set; }
        public decimal? DiskUsagePercent { get; internal set; }
    }
}