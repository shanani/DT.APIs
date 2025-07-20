using DT.EmailWorker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.Entities
{
    /// <summary>
    /// Email history entity for tracking sent emails
    /// </summary>
    public class EmailHistory
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Reference to the original queue item
        /// </summary>
        public Guid QueueId { get; set; }

        // Email Details
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
        /// Email subject
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Final email body (after template processing)
        /// </summary>
        [Required]
        public string FinalBody { get; set; } = string.Empty;

        // Processing Results
        /// <summary>
        /// Final status (Sent or Failed)
        /// </summary>
        public EmailQueueStatus Status { get; set; }

        /// <summary>
        /// When the email was sent
        /// </summary>
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// Whether delivery was confirmed
        /// </summary>
        public bool DeliveryConfirmed { get; set; } = false;

        // Template Info
        /// <summary>
        /// Template ID that was used
        /// </summary>
        public int? TemplateId { get; set; }

        /// <summary>
        /// Template name that was used
        /// </summary>
        [MaxLength(255)]
        public string? TemplateUsed { get; set; }

        // Attachments
        /// <summary>
        /// Number of attachments
        /// </summary>
        public int AttachmentCount { get; set; } = 0;

        /// <summary>
        /// Attachment metadata as JSON
        /// </summary>
        public string? AttachmentMetadata { get; set; }

        // Processing Info
        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public int? ProcessingTimeMs { get; set; }

        /// <summary>
        /// Number of retry attempts before success/failure
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Error details if failed
        /// </summary>
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// Worker ID that processed this email
        /// </summary>
        [MaxLength(100)]
        public string? ProcessedBy { get; set; }

        // Metadata
        /// <summary>
        /// When the history record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// When the record was archived
        /// </summary>
        public DateTime? ArchivedAt { get; set; }

        // Navigation Properties
        /// <summary>
        /// Template reference if a template was used
        /// </summary>
        public virtual EmailTemplate? Template { get; set; }
    }
}