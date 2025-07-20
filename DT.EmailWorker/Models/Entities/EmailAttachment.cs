using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.Entities
{
    /// <summary>
    /// Email attachment entity for storing attachment metadata
    /// </summary>
    public class EmailAttachment
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Reference to the queue item or history record
        /// </summary>
        public Guid QueueId { get; set; }

        /// <summary>
        /// Attachment filename
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Original filename from upload
        /// </summary>
        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// MIME content type
        /// </summary>
        [MaxLength(100)]
        public string? ContentType { get; set; }

        /// <summary>
        /// Content ID for inline attachments (CID)
        /// </summary>
        [MaxLength(100)]
        public string? ContentId { get; set; }

        /// <summary>
        /// Whether this is an inline attachment
        /// </summary>
        public bool IsInline { get; set; } = false;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// File size in bytes (alias for backward compatibility)
        /// </summary>
        public long FileSize
        {
            get => FileSizeBytes;
            set => FileSizeBytes = value;
        }

        /// <summary>
        /// Base64 encoded content
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// File system path (if stored on disk)
        /// </summary>
        [MaxLength(500)]
        public string? FilePath { get; set; }

        /// <summary>
        /// Whether the attachment was processed successfully
        /// </summary>
        public bool IsProcessed { get; set; } = false;

        /// <summary>
        /// Processing error message if any
        /// </summary>
        public string? ProcessingError { get; set; }

        // Metadata
        /// <summary>
        /// When the attachment was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// When the attachment was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(3);
    }
}