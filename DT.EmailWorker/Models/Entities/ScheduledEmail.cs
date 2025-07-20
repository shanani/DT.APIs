using DT.EmailWorker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.Entities
{
    /// <summary>
    /// Scheduled email entity for handling recurring and delayed emails
    /// </summary>
    public class ScheduledEmail
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Unique identifier for the scheduled email
        /// </summary>
        public Guid ScheduleId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name/description of the scheduled email
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Schedule description
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        // Email Content Template
        /// <summary>
        /// Template ID to use for this scheduled email
        /// </summary>
        public int? TemplateId { get; set; }

        /// <summary>
        /// Recipient email addresses (comma-separated)
        /// </summary>
        [Required]
        public string ToEmails { get; set; } = string.Empty;

        /// <summary>
        /// CC email addresses (comma-separated)
        /// </summary>
        public string? CcEmails { get; set; }

        /// <summary>
        /// BCC email addresses (comma-separated)
        /// </summary>
        public string? BccEmails { get; set; }

        /// <summary>
        /// Email subject (can contain placeholders)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body (can contain placeholders)
        /// </summary>
        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Whether the email body is HTML
        /// </summary>
        public bool IsHtml { get; set; } = true;

        /// <summary>
        /// Email priority
        /// </summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;

        // Scheduling Configuration
        /// <summary>
        /// When to start sending this scheduled email
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// When to stop sending this scheduled email (null = no end date)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Next scheduled execution time
        /// </summary>
        public DateTime NextRunTime { get; set; }

        /// <summary>
        /// Cron expression for scheduling (e.g., "0 9 * * MON-FRI" for 9 AM weekdays)
        /// </summary>
        [MaxLength(100)]
        public string? CronExpression { get; set; }

        /// <summary>
        /// Interval in minutes for simple recurring emails
        /// </summary>
        public int? IntervalMinutes { get; set; }

        /// <summary>
        /// Whether this is a recurring email
        /// </summary>
        public bool IsRecurring { get; set; } = false;

        /// <summary>
        /// Whether the scheduled email is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Execution Tracking
        /// <summary>
        /// Number of times this email has been sent
        /// </summary>
        public int ExecutionCount { get; set; } = 0;

        /// <summary>
        /// Maximum number of executions (null = unlimited)
        /// </summary>
        public int? MaxExecutions { get; set; }

        /// <summary>
        /// Last execution time
        /// </summary>
        public DateTime? LastExecutedAt { get; set; }

        /// <summary>
        /// Last execution status
        /// </summary>
        public EmailQueueStatus? LastExecutionStatus { get; set; }

        /// <summary>
        /// Last execution error message
        /// </summary>
        public string? LastExecutionError { get; set; }

        // Template Data
        /// <summary>
        /// Template data as JSON for placeholder replacement
        /// </summary>
        public string? TemplateData { get; set; }

        /// <summary>
        /// Attachments data as JSON array
        /// </summary>
        public string? Attachments { get; set; }

        // Metadata
        /// <summary>
        /// When the scheduled email was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Who created the scheduled email
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// When the scheduled email was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Who last updated the scheduled email
        /// </summary>
        [MaxLength(255)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties
        /// <summary>
        /// Template reference if using a template
        /// </summary>
        public virtual EmailTemplate? Template { get; set; }
    }
}