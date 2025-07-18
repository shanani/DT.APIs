using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Service for managing email template operations and processing
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Process template with placeholders to generate final email content
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="placeholders">Dictionary of placeholder values</param>
        /// <returns>Processed template data</returns>
        Task<TemplateData> ProcessTemplateAsync(int templateId, Dictionary<string, string> placeholders);

        /// <summary>
        /// Process template directly with template content
        /// </summary>
        /// <param name="subjectTemplate">Subject template with placeholders</param>
        /// <param name="bodyTemplate">Body template with placeholders</param>
        /// <param name="placeholders">Dictionary of placeholder values</param>
        /// <returns>Processed template data</returns>
        Task<TemplateData> ProcessTemplateAsync(string subjectTemplate, string bodyTemplate, Dictionary<string, string> placeholders);

        /// <summary>
        /// Get template by ID
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <returns>Email template or null if not found</returns>
        Task<EmailTemplate?> GetTemplateAsync(int templateId);

        /// <summary>
        /// Get template by name
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>Email template or null if not found</returns>
        Task<EmailTemplate?> GetTemplateByNameAsync(string templateName);

        /// <summary>
        /// Get all active templates
        /// </summary>
        /// <returns>List of active templates</returns>
        Task<List<EmailTemplate>> GetActiveTemplatesAsync();

        /// <summary>
        /// Get templates by category
        /// </summary>
        /// <param name="category">Template category</param>
        /// <returns>List of templates in the category</returns>
        Task<List<EmailTemplate>> GetTemplatesByCategoryAsync(string category);

        /// <summary>
        /// Create a new template
        /// </summary>
        /// <param name="template">Template to create</param>
        /// <returns>Created template ID</returns>
        Task<int> CreateTemplateAsync(EmailTemplate template);

        /// <summary>
        /// Update an existing template
        /// </summary>
        /// <param name="template">Template to update</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateTemplateAsync(EmailTemplate template);

        /// <summary>
        /// Delete a template
        /// </summary>
        /// <param name="templateId">Template ID to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteTemplateAsync(int templateId);

        /// <summary>
        /// Activate or deactivate a template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="isActive">Whether the template should be active</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> SetTemplateActiveAsync(int templateId, bool isActive);

        /// <summary>
        /// Validate template syntax and placeholders
        /// </summary>
        /// <param name="subjectTemplate">Subject template to validate</param>
        /// <param name="bodyTemplate">Body template to validate</param>
        /// <returns>Validation result</returns>
        Task<TemplateValidationResult> ValidateTemplateAsync(string subjectTemplate, string bodyTemplate);

        /// <summary>
        /// Extract placeholders from template content
        /// </summary>
        /// <param name="template">Template content</param>
        /// <returns>List of placeholder names found</returns>
        Task<List<string>> ExtractPlaceholdersAsync(string template);

        /// <summary>
        /// Process a simple string template with placeholders
        /// </summary>
        /// <param name="template">Template string with {placeholder} syntax</param>
        /// <param name="placeholders">Dictionary of placeholder values</param>
        /// <returns>Processed string</returns>
        Task<string> ProcessStringTemplateAsync(string template, Dictionary<string, string> placeholders);

        /// <summary>
        /// Clone an existing template
        /// </summary>
        /// <param name="templateId">Template ID to clone</param>
        /// <param name="newName">Name for the cloned template</param>
        /// <param name="createdBy">User creating the clone</param>
        /// <returns>ID of the cloned template</returns>
        Task<int> CloneTemplateAsync(int templateId, string newName, string createdBy);

        /// <summary>
        /// Get template usage statistics
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="days">Number of days to look back</param>
        /// <returns>Usage statistics</returns>
        Task<TemplateUsageStatistics> GetTemplateUsageAsync(int templateId, int days = 30);
    }

    /// <summary>
    /// Template validation result
    /// </summary>
    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Placeholders { get; set; } = new List<string>();
        public int PlaceholderCount { get; set; }
    }

    /// <summary>
    /// Template usage statistics
    /// </summary>
    public class TemplateUsageStatistics
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int TimesUsed { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public DateTime LastUsed { get; set; }
        public double SuccessRate { get; set; }
        public double AverageProcessingTimeMs { get; set; }
    }
}