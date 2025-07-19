using System.ComponentModel.DataAnnotations;

namespace DT.APIs.Models
{
    #region Request Models

    public class QueueEmailRequest
    {
        [Required]
        public string ToEmails { get; set; } = string.Empty;
        public string? CcEmails { get; set; }
        public string? BccEmails { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsHtml { get; set; } = true;
        public int Priority { get; set; } = 2;
        public List<EmailAttachmentRequest> Attachments { get; set; } = new();
        public DateTime? ScheduledFor { get; set; }
        public string? CreatedBy { get; set; }
        public string? RequestSource { get; set; }
    }

    public class QueueTemplateEmailRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        public string ToEmails { get; set; } = string.Empty;
        public string? CcEmails { get; set; }
        public string? BccEmails { get; set; }

        [Required]
        public Dictionary<string, string> TemplateData { get; set; } = new();

        public int Priority { get; set; } = 2;
        public List<EmailAttachmentRequest> Attachments { get; set; } = new();
        public DateTime? ScheduledFor { get; set; }
        public string? CreatedBy { get; set; }
        public string? RequestSource { get; set; }
    }

    public class QueueBulkEmailRequest
    {
        [Required]
        public List<string> Recipients { get; set; } = new();

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsHtml { get; set; } = true;
        public int Priority { get; set; } = 2;
        public bool SendIndividually { get; set; } = true;
        public List<EmailAttachmentRequest> Attachments { get; set; } = new();
        public DateTime? ScheduledFor { get; set; }
        public string? CreatedBy { get; set; }
        public string? RequestSource { get; set; }
    }

    public class EmailAttachmentRequest
    {
        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ContentType { get; set; }
        public string? FilePath { get; set; }
    }

    #endregion

    #region Response Models

    public class QueueEmailResponse
    {
        public Guid QueueId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime? EstimatedProcessingTime { get; set; }
    }

    public class BulkQueueEmailResponse
    {
        public List<QueueEmailResponse> Results { get; set; } = new();
        public int TotalQueued { get; set; }
        public int SuccessfulQueues { get; set; }
        public int FailedQueues { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
    }

    public class EmailStatusResponse
    {
        public Guid QueueId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string ToEmails { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProcessedBy { get; set; }
        public bool HasAttachments { get; set; }
        public bool HasEmbeddedImages { get; set; }
    }

    public class QueueHealthResponse
    {
        public int TotalInQueue { get; set; }
        public int QueuedEmails { get; set; }
        public int ProcessingEmails { get; set; }
        public int FailedEmails { get; set; }
        public int ScheduledEmails { get; set; }
        public double AverageProcessingTimeMinutes { get; set; }
        public double? OldestQueuedEmailMinutes { get; set; }
        public string WorkerServiceStatus { get; set; } = "Unknown";
        public DateTime? LastWorkerActivity { get; set; }
        public string HealthStatus { get; set; } = "Unknown";
    }

    public class QueueStatisticsResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessfulSent { get; set; }
        public int Failed { get; set; }
        public int InQueue { get; set; }
        public int Processing { get; set; }
        public int Scheduled { get; set; }
        public double SuccessRate { get; set; }
        public double AverageProcessingTimeSeconds { get; set; }
    }

    public class PagedEmailQueueResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public List<EmailQueueItem> Items { get; set; } = new();
    }

    public class EmailQueueItem
    {
        public Guid QueueId { get; set; }
        public int Priority { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string ToEmails { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasAttachments { get; set; }
        public string? CreatedBy { get; set; }
        public string? RequestSource { get; set; }
    }

    #endregion
}