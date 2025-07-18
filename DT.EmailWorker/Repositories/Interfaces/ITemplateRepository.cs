using DT.EmailWorker.Models.Entities;

namespace DT.EmailWorker.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for email template operations
    /// </summary>
    public interface ITemplateRepository
    {
        /// <summary>
        /// Get template by ID
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email template or null</returns>
        Task<EmailTemplate?> GetByIdAsync(int templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get template by name
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Email template or null</returns>
        Task<EmailTemplate?> GetByNameAsync(string templateName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all active templates
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active templates</returns>
        Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new template
        /// </summary>
        /// <param name="template">Template to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Added template with ID</returns>
        Task<EmailTemplate> AddAsync(EmailTemplate template, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update existing template
        /// </summary>
        /// <param name="template">Template to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete template
        /// </summary>
        /// <param name="templateId">Template ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteAsync(int templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get templates by category
        /// </summary>
        /// <param name="category">Template category</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of templates in category</returns>
        Task<List<EmailTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search templates by name or description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching templates</returns>
        Task<List<EmailTemplate>> SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get template usage statistics
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Usage count</returns>
        Task<int> GetUsageCountAsync(int templateId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    }
}