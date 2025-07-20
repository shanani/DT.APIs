using System.ComponentModel.DataAnnotations;

namespace DT.EmailWorker.Models.Entities
{
    /// <summary>
    /// Email template entity for reusable email templates
    /// </summary>
    public class EmailTemplate
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Template name (unique)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Template description
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Template category (e.g., "System", "Marketing", "Notification")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        // Template Content (Serialized)
        /// <summary>
        /// Email subject template with placeholders
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string SubjectTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Email body template with placeholders
        /// </summary>
        [Required]
        public string BodyTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Template metadata as JSON
        /// </summary>
        public string? TemplateData { get; set; }

        // Template Settings
        /// <summary>
        /// Whether the template is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this is a system template (cannot be deleted)
        /// </summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// Template version number
        /// </summary>
        public int Version { get; set; } = 1;

        // Metadata
        /// <summary>
        /// When the template was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Who created the template
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// When the template was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Who last updated the template
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string UpdatedBy { get; set; } = string.Empty;

        // Navigation Properties
        /// <summary>
        /// Email queue items using this template
        /// </summary>
        public virtual ICollection<EmailQueue> EmailQueues { get; set; } = new List<EmailQueue>();

        /// <summary>
        /// Email history items that used this template
        /// </summary>
        public virtual ICollection<EmailHistory> EmailHistories { get; set; } = new List<EmailHistory>();
    }
}