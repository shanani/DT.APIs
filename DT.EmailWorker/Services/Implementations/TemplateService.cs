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
            _placeholderRegex = new Regex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);
        }

        public async Task<EmailTemplate?> GetTemplateByIdAsync(int templateId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template by ID {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<EmailTemplate?> GetTemplateByNameAsync(string templateName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template by name {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<TemplateProcessingResult> ProcessTemplateAsync(int templateId, Dictionary<string, string> templateData, CancellationToken cancellationToken = default)
        {
            try
            {
                var template = await GetTemplateByIdAsync(templateId, cancellationToken);
                if (template == null)
                {
                    return new TemplateProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Template with ID {templateId} not found"
                    };
                }

                if (!template.IsActive)
                {
                    return new TemplateProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Template with ID {templateId} is not active"
                    };
                }

                return await ProcessTemplateContentAsync(template.SubjectTemplate, template.BodyTemplate, templateData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template {TemplateId}", templateId);
                return new TemplateProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TemplateProcessingResult> ProcessTemplateByNameAsync(string templateName, Dictionary<string, string> templateData, CancellationToken cancellationToken = default)
        {
            try
            {
                var template = await GetTemplateByNameAsync(templateName, cancellationToken);
                if (template == null)
                {
                    return new TemplateProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Template with name '{templateName}' not found or is not active"
                    };
                }

                return await ProcessTemplateContentAsync(template.SubjectTemplate, template.BodyTemplate, templateData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template by name {TemplateName}", templateName);
                return new TemplateProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailTemplates
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Category)
                    .ThenBy(t => t.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active templates");
                throw;
            }
        }

        public async Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate template
                var validation = await ValidateTemplateAsync(template.SubjectTemplate, template.BodyTemplate);
                if (!validation.IsValid)
                {
                    throw new ArgumentException($"Template validation failed: {string.Join(", ", validation.Errors)}");
                }

                // Check for duplicate name
                var existingTemplate = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Name == template.Name, cancellationToken);

                if (existingTemplate != null)
                {
                    throw new ArgumentException($"Template with name '{template.Name}' already exists");
                }

                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;

                _context.EmailTemplates.Add(template);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created template {TemplateId} with name {TemplateName}",
                    template.Id, template.Name);

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template {TemplateName}", template.Name);
                throw;
            }
        }

        public async Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate template
                var validation = await ValidateTemplateAsync(template.SubjectTemplate, template.BodyTemplate);
                if (!validation.IsValid)
                {
                    throw new ArgumentException($"Template validation failed: {string.Join(", ", validation.Errors)}");
                }

                var existingTemplate = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == template.Id, cancellationToken);

                if (existingTemplate == null)
                {
                    throw new ArgumentException($"Template with ID {template.Id} not found");
                }

                // Check for duplicate name (excluding current template)
                var duplicateName = await _context.EmailTemplates
                    .AnyAsync(t => t.Name == template.Name && t.Id != template.Id, cancellationToken);

                if (duplicateName)
                {
                    throw new ArgumentException($"Template with name '{template.Name}' already exists");
                }

                // Update properties
                existingTemplate.Name = template.Name;
                existingTemplate.Category = template.Category;
                existingTemplate.Description = template.Description;
                existingTemplate.SubjectTemplate = template.SubjectTemplate;
                existingTemplate.BodyTemplate = template.BodyTemplate;
                existingTemplate.IsActive = template.IsActive;                
                existingTemplate.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated template {TemplateId} with name {TemplateName}",
                    template.Id, template.Name);

                return existingTemplate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", template.Id);
                throw;
            }
        }

        private async Task<TemplateProcessingResult> ProcessTemplateContentAsync(string subjectTemplate, string bodyTemplate, Dictionary<string, string> templateData)
        {
            await Task.CompletedTask; // Make method async for consistency

            try
            {
                var result = new TemplateProcessingResult();

                // Validate templates first
                var validation = await ValidateTemplateAsync(subjectTemplate, bodyTemplate);
                if (!validation.IsValid)
                {
                    result.ValidationErrors.AddRange(validation.Errors);
                    result.ErrorMessage = string.Join(", ", validation.Errors);
                    return result;
                }

                // Process subject
                result.ProcessedSubject = ProcessPlaceholders(subjectTemplate, templateData ?? new Dictionary<string, string>());

                // Process body
                result.ProcessedBody = ProcessPlaceholders(bodyTemplate, templateData ?? new Dictionary<string, string>());

                // Check for unresolved placeholders
                var unresolvedSubject = _placeholderRegex.Matches(result.ProcessedSubject).Cast<Match>().Select(m => m.Value).ToList();
                var unresolvedBody = _placeholderRegex.Matches(result.ProcessedBody).Cast<Match>().Select(m => m.Value).ToList();

                if (unresolvedSubject.Any())
                {
                    result.ValidationErrors.Add($"Unresolved placeholders in subject: {string.Join(", ", unresolvedSubject)}");
                }

                if (unresolvedBody.Any())
                {
                    result.ValidationErrors.Add($"Unresolved placeholders in body: {string.Join(", ", unresolvedBody)}");
                }

                result.IsSuccess = !result.ValidationErrors.Any();

                if (!result.IsSuccess)
                {
                    result.ErrorMessage = string.Join(", ", result.ValidationErrors);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template content");
                return new TemplateProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string ProcessPlaceholders(string template, Dictionary<string, string> templateData)
        {
            if (string.IsNullOrWhiteSpace(template))
                return template;

            return _placeholderRegex.Replace(template, match =>
            {
                var placeholder = match.Groups[1].Value;

                // Try exact match first
                if (templateData.TryGetValue(placeholder, out var value))
                {
                    return value ?? string.Empty;
                }

                // Try case-insensitive match
                var caseInsensitiveKey = templateData.Keys
                    .FirstOrDefault(k => string.Equals(k, placeholder, StringComparison.OrdinalIgnoreCase));

                if (caseInsensitiveKey != null)
                {
                    return templateData[caseInsensitiveKey] ?? string.Empty;
                }

                // Return original placeholder if not found
                return match.Value;
            });
        }

        private async Task<TemplateValidationResult> ValidateTemplateAsync(string subjectTemplate, string bodyTemplate)
        {
            await Task.CompletedTask; // Make method async for consistency

            var result = new TemplateValidationResult();

            try
            {
                // Check for empty templates
                if (string.IsNullOrWhiteSpace(subjectTemplate))
                {
                    result.Errors.Add("Subject template cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(bodyTemplate))
                {
                    result.Errors.Add("Body template cannot be empty");
                }

                // Extract and validate placeholders
                var subjectPlaceholders = ExtractPlaceholders(subjectTemplate);
                var bodyPlaceholders = ExtractPlaceholders(bodyTemplate);
                var allPlaceholders = subjectPlaceholders.Union(bodyPlaceholders).ToList();

                result.Placeholders = allPlaceholders;
                result.PlaceholderCount = allPlaceholders.Count;

                // Validate placeholder syntax
                foreach (var placeholder in allPlaceholders)
                {
                    if (string.IsNullOrWhiteSpace(placeholder))
                    {
                        result.Errors.Add("Empty placeholder found");
                    }

                    if (placeholder.Contains("{") || placeholder.Contains("}"))
                    {
                        result.Errors.Add($"Nested brackets not allowed in placeholder: {placeholder}");
                    }
                }

                // Check for malformed brackets
                if (CountBrackets(subjectTemplate) % 2 != 0)
                {
                    result.Errors.Add("Unmatched brackets in subject template");
                }

                if (CountBrackets(bodyTemplate) % 2 != 0)
                {
                    result.Errors.Add("Unmatched brackets in body template");
                }

                result.IsValid = !result.Errors.Any();

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Validation error: {ex.Message}");
                result.IsValid = false;
                return result;
            }
        }

        private List<string> ExtractPlaceholders(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
                return new List<string>();

            return _placeholderRegex.Matches(template)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        private int CountBrackets(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Count(c => c == '{' || c == '}');
        }

        // Additional helper methods for template management
        public async Task<bool> DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default)
        {
            try
            {
                var template = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

                if (template == null)
                    return false;

                // Check if template is being used
                var isUsed = await _context.EmailQueue
                    .AnyAsync(eq => eq.TemplateId == templateId, cancellationToken);

                if (isUsed)
                {
                    // Instead of deleting, mark as inactive
                    template.IsActive = false;
                    template.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.EmailTemplates.Remove(template);
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Template {TemplateId} {'deleted' : 'deactivated'}",
                    templateId, isUsed ? "deactivated" : "deleted");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<TemplateUsageStatistics> GetTemplateUsageStatisticsAsync(int templateId, CancellationToken cancellationToken = default)
        {
            try
            {
                var template = await GetTemplateByIdAsync(templateId, cancellationToken);
                if (template == null)
                {
                    throw new ArgumentException($"Template with ID {templateId} not found");
                }

                var usageStats = await _context.EmailHistory
                    .Where(eh => eh.TemplateId == templateId)
                    .GroupBy(eh => eh.TemplateId)
                    .Select(g => new
                    {
                        TimesUsed = g.Count(),
                        SuccessfulSends = g.Count(eh => eh.SentAt != null),
                        LastUsed = g.Max(eh => eh.CreatedAt)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return new TemplateUsageStatistics
                {
                    TemplateId = templateId,
                    TemplateName = template.Name,
                    TimesUsed = usageStats?.TimesUsed ?? 0,
                    SuccessfulSends = usageStats?.SuccessfulSends ?? 0,
                    FailedSends = (usageStats?.TimesUsed ?? 0) - (usageStats?.SuccessfulSends ?? 0),
                    LastUsed = usageStats?.LastUsed ?? DateTime.MinValue,
                    SuccessRate = usageStats?.TimesUsed > 0 ?
                        (double)(usageStats.SuccessfulSends) / usageStats.TimesUsed * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template usage statistics for {TemplateId}", templateId);
                throw;
            }
        }
    }

    /// <summary>
    /// Template validation result
    /// </summary>
    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
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
    }
}