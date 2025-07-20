using DT.EmailWorker.Models.DTOs;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Core email processing service interface
    /// </summary>
    public interface IEmailProcessingService
    {
        /// <summary>
        /// Process and send email
        /// </summary>
        /// <param name="request">Email processing request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processing result</returns>
        Task<EmailProcessingResult> ProcessEmailAsync(EmailProcessingRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process template-based email
        /// </summary>
        /// <param name="request">Template email request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processing result</returns>
        Task<EmailProcessingResult> ProcessTemplateEmailAsync(TemplateEmailRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate email content and attachments
        /// </summary>
        /// <param name="request">Email processing request</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateEmailAsync(EmailProcessingRequest request);
    }

    /// <summary>
    /// Email processing result
    /// </summary>
    public class EmailProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public long ProcessingTimeMs { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow.AddHours(3);
        public string? MessageId { get; set; }
    }

    /// <summary>
    /// Template email request
    /// </summary>
    public class TemplateEmailRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public string ToEmails { get; set; } = string.Empty;
        public string? CcEmails { get; set; }
        public string? BccEmails { get; set; }
        public Dictionary<string, string> TemplateData { get; set; } = new();
        public List<AttachmentData> Attachments { get; set; } = new();
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}