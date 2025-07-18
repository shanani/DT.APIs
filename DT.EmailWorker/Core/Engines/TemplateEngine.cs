using DT.EmailWorker.Core.Extensions;
using DT.EmailWorker.Models.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DT.EmailWorker.Core.Engines
{
    /// <summary>
    /// Template processing engine for email content
    /// </summary>
    public class TemplateEngine
    {
        private readonly ILogger<TemplateEngine> _logger;

        public TemplateEngine(ILogger<TemplateEngine> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process template with data
        /// </summary>
        public async Task<TemplateProcessingResult> ProcessTemplateAsync(EmailTemplate template, Dictionary<string, string> data)
        {
            try
            {
                var result = new TemplateProcessingResult
                {
                    IsSuccess = true,
                    ProcessedSubject = ProcessPlaceholders(template.Subject, data),
                    ProcessedBody = ProcessPlaceholders(template.Body, data)
                };

                // Process conditional content
                result.ProcessedBody = ProcessConditionalContent(result.ProcessedBody, data);

                // Process loops
                result.ProcessedBody = ProcessLoops(result.ProcessedBody, data);

                // Validate processed content
                var validation = ValidateProcessedContent(result);
                result.ValidationErrors = validation;
                result.IsSuccess = !validation.Any();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process template {TemplateId}", template.Id);
                return new TemplateProcessingResult
                {
                    IsSuccess = false,
                    ValidationErrors = new List<string> { $"Template processing error: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// Process simple placeholders
        /// </summary>
        private string ProcessPlaceholders(string content, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(content) || data == null || !data.Any())
                return content;

            return content.ReplacePlaceholders(data);
        }

        /// <summary>
        /// Process conditional content blocks
        /// </summary>
        private string ProcessConditionalContent(string content, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Pattern: {{#if condition}}content{{/if}}
            var conditionalRegex = new Regex(@"\{\{#if\s+(\w+)\}\}(.*?)\{\{/if\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return conditionalRegex.Replace(content, match =>
            {
                var condition = match.Groups[1].Value;
                var conditionContent = match.Groups[2].Value;

                // Check if condition exists and is not empty/false
                if (data.TryGetValue(condition, out var value) &&
                    !string.IsNullOrEmpty(value) &&
                    !value.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return conditionContent;
                }

                return string.Empty;
            });
        }

        /// <summary>
        /// Process loop content blocks
        /// </summary>
        private string ProcessLoops(string content, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Pattern: {{#each items}}{{name}}{{/each}}
            var loopRegex = new Regex(@"\{\{#each\s+(\w+)\}\}(.*?)\{\{/each\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return loopRegex.Replace(content, match =>
            {
                var listName = match.Groups[1].Value;
                var loopContent = match.Groups[2].Value;

                // Look for list data (expecting JSON-like format or count)
                if (data.TryGetValue($"{listName}_count", out var countStr) && int.TryParse(countStr, out var count))
                {
                    var result = new System.Text.StringBuilder();
                    for (int i = 0; i < count; i++)
                    {
                        var itemContent = loopContent;
                        // Replace item-specific placeholders
                        foreach (var kvp in data.Where(d => d.Key.StartsWith($"{listName}_{i}_")))
                        {
                            var itemKey = kvp.Key.Substring($"{listName}_{i}_".Length);
                            itemContent = itemContent.Replace($"{{{{{itemKey}}}}}", kvp.Value);
                        }
                        result.Append(itemContent);
                    }
                    return result.ToString();
                }

                return string.Empty;
            });
        }

        /// <summary>
        /// Validate processed content
        /// </summary>
        private List<string> ValidateProcessedContent(TemplateProcessingResult result)
        {
            var errors = new List<string>();

            // Check for unprocessed placeholders
            var subjectPlaceholders = ExtractUnprocessedPlaceholders(result.ProcessedSubject);
            var bodyPlaceholders = ExtractUnprocessedPlaceholders(result.ProcessedBody);

            if (subjectPlaceholders.Any())
            {
                errors.Add($"Unprocessed placeholders in subject: {string.Join(", ", subjectPlaceholders)}");
            }

            if (bodyPlaceholders.Any())
            {
                errors.Add($"Unprocessed placeholders in body: {string.Join(", ", bodyPlaceholders)}");
            }

            // Validate HTML if body contains HTML
            if (result.ProcessedBody.Contains("<") && result.ProcessedBody.Contains(">"))
            {
                var htmlErrors = ValidateHtml(result.ProcessedBody);
                errors.AddRange(htmlErrors);
            }

            return errors;
        }

        /// <summary>
        /// Extract unprocessed placeholders
        /// </summary>
        private List<string> ExtractUnprocessedPlaceholders(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<string>();

            return content.ExtractPlaceholders();
        }

        /// <summary>
        /// Basic HTML validation
        /// </summary>
        private List<string> ValidateHtml(string html)
        {
            var errors = new List<string>();

            try
            {
                // Check for basic HTML structure issues
                var openTags = Regex.Matches(html, @"<(\w+)(?:\s+[^>]*)?>", RegexOptions.IgnoreCase);
                var closeTags = Regex.Matches(html, @"</(\w+)>", RegexOptions.IgnoreCase);

                var openTagCounts = openTags.Cast<Match>()
                    .Select(m => m.Groups[1].Value.ToLower())
                    .Where(tag => !IsSelfClosingTag(tag))
                    .GroupBy(tag => tag)
                    .ToDictionary(g => g.Key, g => g.Count());

                var closeTagCounts = closeTags.Cast<Match>()
                    .Select(m => m.Groups[1].Value.ToLower())
                    .GroupBy(tag => tag)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var openTag in openTagCounts)
                {
                    if (!closeTagCounts.TryGetValue(openTag.Key, out var closeCount) || closeCount != openTag.Value)
                    {
                        errors.Add($"Mismatched HTML tags for: {openTag.Key}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"HTML validation error: {ex.Message}");
            }

            return errors;
        }

        /// <summary>
        /// Check if tag is self-closing
        /// </summary>
        private bool IsSelfClosingTag(string tag)
        {
            var selfClosingTags = new[] { "br", "hr", "img", "input", "meta", "link", "area", "base", "col", "embed", "source", "track", "wbr" };
            return selfClosingTags.Contains(tag.ToLower());
        }

        /// <summary>
        /// Get required placeholders from template
        /// </summary>
        public List<string> GetRequiredPlaceholders(EmailTemplate template)
        {
            var placeholders = new HashSet<string>();

            if (!string.IsNullOrEmpty(template.Subject))
            {
                placeholders.UnionWith(template.Subject.ExtractPlaceholders());
            }

            if (!string.IsNullOrEmpty(template.Body))
            {
                placeholders.UnionWith(template.Body.ExtractPlaceholders());
            }

            return placeholders.ToList();
        }
    }

    /// <summary>
    /// Template processing result
    /// </summary>
    public class TemplateProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string ProcessedSubject { get; set; } = string.Empty;
        public string ProcessedBody { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
    }
}