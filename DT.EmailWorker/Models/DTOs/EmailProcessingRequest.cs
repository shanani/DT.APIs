using DT.EmailWorker.Models.Enums;

namespace DT.EmailWorker.Models.DTOs
{
    /// <summary>
    /// DTO for email processing requests from the queue
    /// </summary>
    public class EmailProcessingRequest
    {
        /// <summary>
        /// Queue ID for tracking
        /// </summary>
        public Guid QueueId { get; set; }

        /// <summary>
        /// Email priority
        /// </summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;

        // Email Content
        /// <summary>
        /// Recipient email addresses (comma-separated)
        /// </summary>
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
        /// Email subject
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body content
        /// </summary>
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
        /// Template data as JSON for placeholder replacement
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
        /// Maximum retry attempts allowed
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Who created the email request
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Source of the request (API, WebApp, SQLJob, etc.)
        /// </summary>
        public string? RequestSource { get; set; }

        /// <summary>
        /// Correlation ID for tracking related operations
        /// </summary>
        public Guid? CorrelationId { get; set; }
    }
}