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
        /