using DT.EmailWorker.Models.Entities;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Service for managing email template operations and processing
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Get template by ID
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email template or null</returns>
        Task<EmailTemplate?> GetTemplateByIdAsync(int templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get template by name
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email template or null</returns>
        Task<EmailTemplate?> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process template with data
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="templateData">Template data for placeholder replacement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Template processing result</returns>
        Task<TemplateProcessingResult> ProcessTemplateAsync(int templateId, Dictionary<string, string> templateData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process template by name with data
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <param name="templateData">Template data for placeholder replacement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Template processing result</returns>
        Task<TemplateProcessingResult> ProcessTemplateByNameAsync(string templateName, Dictionary<string, string> templateData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all active templates
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active templates</returns>
        Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Create new template
        /// </summary>
        /// <param name="template">Template to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created template</returns>
        Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update existing template
        /// </summary>
        /// <param name="template">Template to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated template</returns>
        Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Template processing result
    /// </summary>
    public class TemplateProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string ProcessedSubject { get; set; } = string.Empty;
        public string ProcessedBody { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }
}