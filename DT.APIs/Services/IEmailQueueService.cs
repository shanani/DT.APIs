using DT.APIs.Models;

namespace DT.APIs.Services
{
    public interface IEmailQueueService
    {
        // Queue Operations
        Task<QueueEmailResponse> QueueEmailAsync(QueueEmailRequest request);
        Task<QueueEmailResponse> QueueTemplateEmailAsync(QueueTemplateEmailRequest request);
        Task<BulkQueueEmailResponse> QueueBulkEmailAsync(QueueBulkEmailRequest request);

        // Status Management
        Task<EmailStatusResponse?> GetEmailStatusAsync(Guid queueId);
        Task<List<EmailStatusResponse>> GetBatchEmailStatusAsync(List<Guid> queueIds);
        Task<bool> CancelEmailAsync(Guid queueId);

        // Monitoring
        Task<QueueHealthResponse> GetQueueHealthAsync();
        Task<QueueStatisticsResponse> GetQueueStatisticsAsync(DateTime? fromDate, DateTime? toDate);

        // Queue Management
        Task<PagedEmailQueueResponse> GetQueuedEmailsAsync(int page, int pageSize, string? status = null, string? priority = null, DateTime? fromDate = null, DateTime? toDate = null, string? search = null);
    }
}