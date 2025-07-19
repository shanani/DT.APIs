using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.DTOs
{
    /// <summary>
    /// DTO for email attachment data transfer
    /// </summary>
    public class AttachmentData
    {
        /// <summary>
        /// Attachment filename
        /// </summary>
        [Required]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Original filename from upload
        /// </summary>
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// MIME content type
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Content ID for inline attachments (CID)
        /// </summary>
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
        /// Base64 encoded content
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// File system path (if stored on disk)
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Additional metadata as JSON
        /// </summary>
        public string? Metadata { get; set; }
    }
}