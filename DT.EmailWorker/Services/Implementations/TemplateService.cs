using DT.EmailWorker.Data;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// Implementation of email template processing service
    /// </summary>
    public class TemplateService : ITemplateService
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<TemplateService> _logger;
        private readonly Regex _placeholderRegex;

        public TemplateService(EmailDbContext context, ILogger<TemplateService> logger)
        {
            _context = context;
            _logger = logger;
            _placeholderRegex = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);
        }

        public async Task<TemplateData> ProcessTemplateAsync(int templateId, Dictionary<string, string> placeholders)
        {
            try
            {
                var template = await GetTemplateAsync(templateId);
                if (template == null)
                {
                    return new TemplateData
                    {
                        TemplateId = templateId,
                        ProcessingSuccessful = false,
                        ProcessingError = $"Template with ID {templateId} not found"
                    };
                }

                return await ProcessTemplateAsync(template.SubjectTemplate, template.BodyTemplate, placeholders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template {TemplateId}", templateId);
                return new TemplateData
                {
                    TemplateId = templateId,
                    ProcessingSuccessful = false,
                    ProcessingError = ex.Message
                };
            }
        }

        public async Task<TemplateData> ProcessTemplateAsync(string subjectTemplate, string bodyTemplate, Dictionary<string, string> placeholders)
        {
            try
            {
                var templateData = new TemplateData
                {
                    SubjectTemplate = subjectTemplate,
                    BodyTemplate = bodyTemplate,
                    Placeholders = placeholders ?? new Dictionary<string, string>()
                };

                // Extract placeholders from templates
                var subjectPlaceholders = await ExtractPlaceholdersAsync(subjectTemplate);
                var bodyPlaceholders = await ExtractPlaceholdersAsync(bodyTemplate);
                var allPlaceholders = subjectPlaceholders.Union(bodyPlaceholders).ToList();

                templateData.MissingPlaceholders = allPlaceholders
                    .Where(p => !placeholders.ContainsKey(p))
                    .ToList();

                // Process subject
                templateData.ProcessedSubject = await ProcessStringTemplateAsync(subjectTemplate, placeholders);

                // Process body
                templateData.ProcessedBody = await ProcessStringTemplateAsync(bodyTemplate, placeholders);

                templateData.ProcessingSuccessful = true;

                if (templateData.MissingPlaceholders.Any())
                {
                    _logger.LogWarning("Template processing completed with missing placeholders: {MissingPlaceholders}",
                        string.Join(", ", templateData.MissingPlaceholders));
                }

                return templateData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template");
                return new TemplateData
                {
                    SubjectTemplate = subjectTemplate,
                    BodyTemplate = bodyTemplate,
                    ProcessingSuccessful = false,
                    ProcessingError = ex.Message
                };
            }
        }

        public async Task<EmailTemplate?> GetTemplateAsync(int templateId)
        {
            try
            {
                return await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<EmailTemplate?> GetTemplateByNameAsync(string templateName)
        {
            try
            {
                return await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template by name {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<List<EmailTemplate>> GetActiveTemplatesAsync()
        {
            try
            {
                return await _context.EmailTemplates
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Category)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active templates");
                throw;
            }
        }

        public async Task<List<EmailTemplate>> GetTemplatesByCategoryAsync(string category)
        {
            try
            {
                return await _context.EmailTemplates
                    .Where(t => t.Category == category && t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates by category {Category}", category);
                throw;
            }
        }

        public async Task<int> CreateTemplateAsync(EmailTemplate template)
        {
            try
            {
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;
                template.UpdatedBy = template.CreatedBy;

                _context.EmailTemplates.Add(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created template {TemplateName} with ID {TemplateId}",
                    template.Name, template.Id);

                return template.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template {TemplateName}", template.Name);
                throw;
            }
        }

        public async Task<bool> UpdateTemplateAsync(EmailTemplate template)
        {
            try
            {
                var existingTemplate = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == template.Id);

                if (existingTemplate == null)
                    return false;

                existingTemplate.Name = template.Name;
                existingTemplate.Description = template.Description;
                existingTemplate.Category = template.Category;
                existingTemplate.SubjectTemplate = template.SubjectTemplate;
                existingTemplate.BodyTemplate = template.BodyTemplate;
                existingTemplate.TemplateData = template.TemplateData;
                existingTemplate.IsActive = template.IsActive;
                existingTemplate.Version++;
                existingTemplate.UpdatedAt = DateTime.UtcNow;
                existingTemplate.UpdatedBy = template.UpdatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated template {TemplateName} (ID: {TemplateId})",
                    template.Name, template.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", template.Id);
                throw;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null || template.IsSystem)
                    return false;

                _context.EmailTemplates.Remove(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted template {TemplateId}", templateId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<bool> SetTemplateActiveAsync(int templateId, bool isActive)
        {
            try
            {
                var template = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                    return false;

                template.IsActive = isActive;
                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Set template {TemplateId} active status to {IsActive}",
                    templateId, isActive);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting template active status {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<TemplateValidationResult> ValidateTemplateAsync(string subjectTemplate, string bodyTemplate)
        {
            try
            {
                var result = new TemplateValidationResult();

                // Extract placeholders
                var subjectPlaceholders = await ExtractPlaceholdersAsync(subjectTemplate);
                var bodyPlaceholders = await ExtractPlaceholdersAsync(bodyTemplate);
                var allPlaceholders = subjectPlaceholders.Union(bodyPlaceholders).ToList();

                result.Placeholders = allPlaceholders;
                result.PlaceholderCount = allPlaceholders.Count;

                // Validate placeholder syntax
                if (subjectPlaceholders.Count != _placeholderRegex.Matches(subjectTemplate).Count)
                {
                    result.Errors.Add("Subject template contains malformed placeholders");
                }

                if (bodyPlaceholders.Count != _placeholderRegex.Matches(bodyTemplate).Count)
                {
                    result.Errors.Add("Body template contains malformed placeholders");
                }

                // Check for common issues
                if (string.IsNullOrWhiteSpace(subjectTemplate))
                {
                    result.Errors.Add("Subject template cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(bodyTemplate))
                {
                    result.Errors.Add("Body template cannot be empty");
                }

                // Check for potential HTML issues in body
                if (bodyTemplate.Contains("<script"))
                {
                    result.Warnings.Add("Body template contains script tags which may be blocked by email clients");
                }

                if (bodyTemplate.Contains("javascript:"))
                {
                    result.Warnings.Add("Body template contains JavaScript which may be blocked by email clients");
                }

                result.IsValid = !result.Errors.Any();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template");
                return new TemplateValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<List<string>> ExtractPlaceholdersAsync(string template)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(template))
                    return new List<string>();

                var matches = _placeholderRegex.Matches(template);
                return matches.Cast<Match>()
                    .Select(m => m.Groups[1].Value.Trim())
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting placeholders from template");
                throw;
            }
        }

        public async Task<string> ProcessStringTemplateAsync(string template, Dictionary<string, string> placeholders)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(template))
                    return string.Empty;

                if (placeholders == null || !placeholders.Any())
                    return template;

                var result = template;

                foreach (var placeholder in placeholders)
                {
                    var pattern = $"{{{placeholder.Key}}}";
                    result = result.Replace(pattern, placeholder.Value ?? string.Empty);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing string template");
                throw;
            }
        }

        public async Task<int> CloneTemplateAsync(int templateId, string newName, string createdBy)
        {
            try
            {
                var sourceTemplate = await GetTemplateAsync(templateId);
                if (sourceTemplate == null)
                    throw new ArgumentException($"Template {templateId} not found");

                var clonedTemplate = new EmailTemplate
                {
                    Name = newName,
                    Description = $"Cloned from {sourceTemplate.Name}",
                    Category = sourceTemplate.Category,
                    SubjectTemplate = sourceTemplate.SubjectTemplate,
                    BodyTemplate = sourceTemplate.BodyTemplate,
                    TemplateData = sourceTemplate.TemplateData,
                    IsActive = true,
                    IsSystem = false,
                    Version = 1,
                    CreatedBy = createdBy
                };

                return await CreateTemplateAsync(clonedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<TemplateUsageStatistics> GetTemplateUsageAsync(int templateId, int days = 30)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);

                var template = await GetTemplateAsync(templateId);
                if (template == null)
                    throw new ArgumentException($"Template {templateId} not found");

                var usage = await _context.EmailHistory
                    .Where(h => h.TemplateId == templateId && h.CreatedAt >= startDate)
                    .GroupBy(h => h.TemplateId)
                    .Select(g => new
                    {
                        TimesUsed = g.Count(),
                        SuccessfulSends = g.Count(h => h.Status == Models.Enums.EmailQueueStatus.Sent),
                        FailedSends = g.Count(h => h.Status == Models.Enums.EmailQueueStatus.Failed),
                        LastUsed = g.Max(h => h.CreatedAt),
                        AverageProcessingTime = g.Average(h => h.ProcessingTimeMs ?? 0)
                    })
                    .FirstOrDefaultAsync();

                return new TemplateUsageStatistics
                {
                    TemplateId = templateId,
                    TemplateName = template.Name,
                    TimesUsed = usage?.TimesUsed ?? 0,
                    SuccessfulSends = usage?.SuccessfulSends ?? 0,
                    FailedSends = usage?.FailedSends ?? 0,
                    LastUsed = usage?.LastUsed ?? DateTime.MinValue,
                    SuccessRate = usage?.TimesUsed > 0 ? (double)(usage.SuccessfulSends) / usage.TimesUsed * 100 : 0,
                    AverageProcessingTimeMs = usage?.AverageProcessingTime ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template usage statistics for {TemplateId}", templateId);
                throw;
            }
        }
    }
}