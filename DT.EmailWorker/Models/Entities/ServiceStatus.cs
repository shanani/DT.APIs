using DT.EmailWorker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.Entities
{
    /// <summary>
    /// Service status entity for real-time health and performance tracking
    /// </summary>
    public class ServiceStatus
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Service name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Machine name where the service is running
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string MachineName { get; set; } = Environment.MachineName;

        // Health Status
        /// <summary>
        /// Current health status
        /// </summary>
        public ServiceHealthStatus Status { get; set; } = ServiceHealthStatus.Healthy;

        /// <summary>
        /// Last heartbeat timestamp
        /// </summary>
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

        // Performance Metrics
        /// <summary>
        /// Current queue depth (number of pending emails)
        /// </summary>
        public int QueueDepth { get; set; } = 0;

        /// <summary>
        /// Emails processed per hour
        /// </summary>
        public int EmailsProcessedPerHour { get; set; } = 0;

        /// <summary>
        /// Error rate percentage (0.00 to 100.00)
        /// </summary>
        public decimal ErrorRate { get; set; } = 0.0m;

        /// <summary>
        /// Average processing time in milliseconds
        /// </summary>
        public decimal? AverageProcessingTimeMs { get; set; }

        // Resource Usage
        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public decimal? CpuUsagePercent { get; set; }

        /// <summary>
        /// Memory usage in MB
        /// </summary>
        public decimal? MemoryUsageMB { get; set; }

        /// <summary>
        /// Disk usage percentage
        /// </summary>
        public decimal? DiskUsagePercent { get; set; }

        // Configuration
        /// <summary>
        /// Maximum concurrent workers configured
        /// </summary>
        public int MaxConcurrentWorkers { get; set; }

        /// <summary>
        /// Current number of active workers
        /// </summary>
        public int CurrentActiveWorkers { get; set; } = 0;

        /// <summary>
        /// Batch size for processing
        /// </summary>
        public int BatchSize { get; set; }

        // Version and Status Info
        /// <summary>
        /// Service version
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ServiceVersion { get; set; } = string.Empty;

        /// <summary>
        /// When the service was started
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the status was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last error message if any
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// When the last error occurred
        /// </summary>
        public DateTime? LastErrorAt { get; set; }

        // Statistics
        /// <summary>
        /// Total emails processed since service start
        /// </summary>
        public long TotalEmailsProcessed { get; set; } = 0;

        /// <summary>
        /// Total emails failed since service start
        /// </summary>
        public long TotalEmailsFailed { get; set; } = 0;

        /// <summary>
        /// Service uptime in seconds
        /// </summary>
        public long UptimeSeconds { get; set; } = 0;
    }
}