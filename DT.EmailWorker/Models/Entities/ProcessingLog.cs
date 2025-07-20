using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.Entities
{
    /// <summary>
    /// Processing log entity for detailed service operation logging
    /// </summary>
    public class ProcessingLog
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Log level (Debug=1, Info=2, Warning=3, Error=4, Critical=5)
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Log category (e.g., "EmailProcessing", "QueueManagement")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Log message
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Exception details if applicable
        /// </summary>
        public string? Exception { get; set; }

        // Context
        /// <summary>
        /// Queue ID if related to a specific email
        /// </summary>
        public Guid? QueueId { get; set; }

        /// <summary>
        /// Worker ID that generated this log
        /// </summary>
        [MaxLength(100)]
        public string? WorkerId { get; set; }

        /// <summary>
        /// Processing step when this log was created
        /// </summary>
        [MaxLength(100)]
        public string? ProcessingStep { get; set; }

        /// <summary>
        /// Additional context data as JSON
        /// </summary>
        public string? ContextData { get; set; }

        /// <summary>
        /// Correlation ID for tracking related operations
        /// </summary>
        public Guid? CorrelationId { get; set; }

        // Metadata
        /// <summary>
        /// When the log was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Machine name where the log was generated
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string MachineName { get; set; } = Environment.MachineName;

        // Navigation Properties
        /// <summary>
        /// Related email queue item if applicable
        /// </summary>
        public virtual EmailQueue? EmailQueue { get; set; }
    }
}