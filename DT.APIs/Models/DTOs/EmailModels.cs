using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Swashbuckle.AspNetCore.Annotations;

namespace DT.APIs.Models
{
    public class EmailModel
    {
        [Required]
        [SwaggerSchema(Description = "Email subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [SwaggerSchema(Description = "Primary recipient email")]
        public string RecipientEmail { get; set; } = string.Empty;

        [SwaggerSchema(Description = "Recipient display name")]
        public string? RecipientName { get; set; }

        [Required]
        [SwaggerSchema(Description = "Email body content")]
        public string Body { get; set; } = string.Empty;

        [SwaggerSchema(Description = "CC recipients")]
        public List<string> CC { get; set; } = new List<string>();

        [SwaggerSchema(Description = "BCC recipients")]
        public List<string> BCC { get; set; } = new List<string>();

        [SwaggerSchema(Description = "Reply-to email address")]
        [EmailAddress]
        public string? ReplyTo { get; set; }

        [SwaggerSchema(Description = "Email priority (Low=0, Normal=1, High=2)")]
        public MailPriority Priority { get; set; } = MailPriority.Normal;

        [SwaggerSchema(Description = "Is email body HTML formatted")]
        public bool IsBodyHtml { get; set; } = true;

        [SwaggerSchema(Description = "File attachments (Base64 encoded)")]
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();

        [SwaggerSchema(Description = "Custom headers")]
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();

        [SwaggerSchema(Description = "Request delivery notification")]
        public bool RequestDeliveryNotification { get; set; } = false;

        [SwaggerSchema(Description = "Request read receipt")]
        public bool RequestReadReceipt { get; set; } = false;
    }

    public class EmailAttachment
    {
        [SwaggerSchema(Description = "File name with extension")]
        public string FileName { get; set; } = string.Empty;

        [SwaggerSchema(Description = "Base64 encoded file content")]
        public string Content { get; set; } = string.Empty;

        [SwaggerSchema(Description = "MIME content type (e.g., application/pdf, image/png)")]
        public string? ContentType { get; set; }

        [SwaggerSchema(Description = "Content ID for inline attachments")]
        public string? ContentId { get; set; }

        [SwaggerSchema(Description = "Is this an inline attachment")]
        public bool IsInline { get; set; } = false;
    }

    public class BulkEmailModel
    {
        [Required]
        [SwaggerSchema(Description = "Email subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [SwaggerSchema(Description = "Email body content")]
        public string Body { get; set; } = string.Empty;

        [Required]
        [SwaggerSchema(Description = "List of recipient emails")]
        public List<string> Recipients { get; set; } = new List<string>();

        [SwaggerSchema(Description = "CC recipients for all emails")]
        public List<string> CC { get; set; } = new List<string>();

        [SwaggerSchema(Description = "BCC recipients for all emails")]
        public List<string> BCC { get; set; } = new List<string>();

        [SwaggerSchema(Description = "Reply-to email address")]
        public string? ReplyTo { get; set; }

        [SwaggerSchema(Description = "Email priority")]
        public MailPriority Priority { get; set; } = MailPriority.Normal;

        [SwaggerSchema(Description = "Is email body HTML formatted")]
        public bool IsBodyHtml { get; set; } = true;

        [SwaggerSchema(Description = "File attachments")]
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();

        [SwaggerSchema(Description = "Send emails individually (true) or as one email with multiple recipients (false)")]
        public bool SendIndividually { get; set; } = true;
    }

    public class BulkEmailResultModel
    {
        public int TotalEmails { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public List<EmailSendResult> Results { get; set; } = new List<EmailSendResult>();
        public TimeSpan TotalProcessingTime { get; set; }
    }

    public class EmailSendResult
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class EmailTemplateModel
    {
        [Required]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public Dictionary<string, string> Placeholders { get; set; } = new Dictionary<string, string>();
    }
}