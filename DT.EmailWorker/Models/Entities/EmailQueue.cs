using DT.EmailWorker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.Entities
{
    /// <summary>
    /// Email queue entity for storing emails to be processed
    /// </summary>
    public class EmailQueue
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Unique queue identifier
        /// </summary>
        public Guid QueueId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Email priority (1=Low, 2=Normal, 3=High, 4=Critical)
        /// </summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;

        /// <summary>
        /// Current status of the email
        /// </summary>
        public EmailQueueStatus Status { get; set; } = EmailQueueStatus.Queued;

        // Email Content
        /// <summary>
        /// Recipient email addresses (comma-separated)
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string ToEmails { get; set; } = string.Empty;

        /// <summary>
        /// CC email addresses (comma-separated)
        /// </summary>
        [MaxLength(4000)]
        public string? CcEmails { get; set; }

        /// <summary>
        /// BCC email addresses (comma-separated)
        /// </summary>
        [MaxLength(4000)]
        public string? BccEmails { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body content
        /// </summary>
        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Whether the email body is HTML
        /// </summary>
        public bool IsHtml { get; set; } = true;

        // Template Processing
        /// <summary>
        /// Template ID if using a template
        /// </summary>
        public int? TemplateId { get; set; }

        /// <summary>
        /// Template data as JSON
        /// </summary>
        public string? TemplateData { get; set; }

        /// <summary>
        /// Whether this email requires template processing
        /// </summary>
        public bool RequiresTemplateProcessing { get; set; } = false;

        // Attachments
        /// <summary>
        /// Attachments data as JSON array
        /// </summary>
        public string? Attachments { get; set; }

        /// <summary>
        /// Whether the email has embedded images
        /// </summary>
        public bool HasEmbeddedImages { get; set; } = false;

        // Processing Info
        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// When processing started
        /// </summary>
        public DateTime? ProcessingStartedAt { get; set; }

        /// <summary>
        /// When the email was processed
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Worker ID that processed this email
        /// </summary>
        [MaxLength(100)]
        public string? ProcessedBy { get; set; }

        // Scheduling
        /// <summary>
        /// When the email should be sent (for scheduled emails)
        /// </summary>
        public DateTime? ScheduledFor { get; set; }

        /// <summary>
        /// Whether this is a scheduled email
        /// </summary>
        public bool IsScheduled { get; set; } = false;

        // Metadata
        /// <summary>
        /// When the email was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the email was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created the email request
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Source of the request (API, WebApp, SQLJob, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? RequestSource { get; set; }

        // Navigation Properties
        /// <summary>
        /// Template reference if using a template
        /// </summary>
        public virtual EmailTemplate? Template { get; set; }

        /// <summary>
        /// Processing logs for this email
        /// </summary>
        public virtual ICollection<ProcessingLog> ProcessingLogs { get; set; } = new List<ProcessingLog>();
    }
}