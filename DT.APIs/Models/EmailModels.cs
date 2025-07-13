using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Swashbuckle.AspNetCore.Annotations;

namespace DT.APIs.Models
{
    public class EmailModel
    {
        [Required]
        [SwaggerSchema(Description = "Email subject")]
        public string Subject { get; set; }

        [Required]
        [EmailAddress]
        [SwaggerSchema(Description = "Primary recipient email")]
        public string RecipientEmail { get; set; }

        [SwaggerSchema(Description = "Recipient display name")]
        public string? RecipientName { get; set; }

        [Required]
        [SwaggerSchema(Description = "Email body content")]
        public string Body { get; set; }

        [SwaggerSchema(Description = "CC recipients")]
        public List<string>? CC { get; set; }

        [SwaggerSchema(Description = "BCC recipients")]
        public List<string>? BCC { get; set; }

        [SwaggerSchema(Description = "Reply-to email address")]
        [EmailAddress]
        public string? ReplyTo { get; set; }

        [SwaggerSchema(Description = "Email priority (Low=0, Normal=1, High=2)")]
        public MailPriority Priority { get; set; } = MailPriority.Normal;

        [SwaggerSchema(Description = "Is email body HTML formatted")]
        public bool IsBodyHtml { get; set; } = true;

        [SwaggerSchema(Description = "File attachments (Base64 encoded)")]
        public List<EmailAttachment>? Attachments { get; set; }

        [SwaggerSchema(Description = "Custom headers")]
        public Dictionary<string, string>? CustomHeaders { get; set; }

        [SwaggerSchema(Description = "Request delivery notification")]
        public bool RequestDeliveryNotification { get; set; } = false;

        [SwaggerSchema(Description = "Request read receipt")]
        public bool RequestReadReceipt { get; set; } = false;

        public EmailModel()
        {
            CC = new List<string>();
            BCC = new List<string>();
            Attachments = new List<EmailAttachment>();
            CustomHeaders = new Dictionary<string, string>();
        }
    }

    public class EmailAttachment
    {
        [Required]
        [SwaggerSchema(Description = "File name with extension")]
        public string FileName { get; set; }

        [Required]
        [SwaggerSchema(Description = "Base64 encoded file content")]
        public string Content { get; set; }

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
        public string Subject { get; set; }

        [Required]
        [SwaggerSchema(Description = "Email body content")]
        public string Body { get; set; }

        [Required]
        [SwaggerSchema(Description = "List of recipient emails")]
        public List<string> Recipients { get; set; }

        [SwaggerSchema(Description = "CC recipients for all emails")]
        public List<string>? CC { get; set; }

        [SwaggerSchema(Description = "BCC recipients for all emails")]
        public List<string>? BCC { get; set; }

        [SwaggerSchema(Description = "Email priority")]
        public MailPriority Priority { get; set; } = MailPriority.Normal;

        [SwaggerSchema(Description = "Is email body HTML formatted")]
        public bool IsBodyHtml { get; set; } = true;

        [SwaggerSchema(Description = "File attachments")]
        public List<EmailAttachment>? Attachments { get; set; }

        [SwaggerSchema(Description = "Send emails individually (true) or as one email with multiple recipients (false)")]
        public bool SendIndividually { get; set; } = true;

        public BulkEmailModel()
        {
            Recipients = new List<string>();
            CC = new List<string>();
            BCC = new List<string>();
            Attachments = new List<EmailAttachment>();
        }
    }

    public class BulkEmailResultModel
    {
        public int TotalEmails { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public List<EmailSendResult> Results { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }

        public BulkEmailResultModel()
        {
            Results = new List<EmailSendResult>();
        }
    }

    public class EmailSendResult
    {
        public string RecipientEmail { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class EmailTemplateModel
    {
        [Required]
        public string TemplateName { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; }

        public Dictionary<string, string>? Placeholders { get; set; }

        public EmailTemplateModel()
        {
            Placeholders = new Dictionary<string, string>();
        }
    }
}