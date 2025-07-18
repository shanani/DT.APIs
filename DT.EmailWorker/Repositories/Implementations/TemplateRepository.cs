using DT.EmailWorker.Data;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT.EmailWorker.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for email template operations
    /// </summary>
    public class TemplateRepository : ITemplateRepository
    {
        private readonly EmailDbContext _context;
        private readonly ILogger<TemplateRepository> _logger;

        public TemplateRepository(EmailDbContext context, ILogger<TemplateRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailTemplate?> GetByIdAsync(int templateId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get template by ID {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<EmailTemplate?> GetByNameAsync(string templateName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get template by name {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailTemplates
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active templates");
                throw;
            }
        }

        public async Task<EmailTemplate> AddAsync(EmailTemplate template, CancellationToken cancellationToken = default)
        {
            try
            {
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;

                _context.EmailTemplates.Add(template);
                await _context.SaveChangesAsync(cancellationToken);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add template {TemplateName}", template.Name);
                throw;
            }
        }

        public async Task UpdateAsync(EmailTemplate template, CancellationToken cancellationToken = default)
        {
            try
            {
                template.UpdatedAt = DateTime.UtcNow;

                _context.EmailTemplates.Update(template);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update template {TemplateId}", template.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int templateId, CancellationToken cancellationToken = default)
        {
            try
            {
                var template = await _context.EmailTemplates.FindAsync(new object[] { templateId }, cancellationToken);
                if (template != null)
                {
                    // Soft delete by setting IsActive to false
                    template.IsActive = false;
                    template.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<List<EmailTemplate>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailTemplates
                    .Where(t => t.Category == category && t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get templates by category {Category}", category);
                throw;
            }
        }

        public async Task<List<EmailTemplate>> SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                var lowerSearchTerm = searchTerm.ToLower();

                return await _context.EmailTemplates
                    .Where(t => t.IsActive &&
                               (t.Name.ToLower().Contains(lowerSearchTerm) ||
                                t.Description != null && t.Description.ToLower().Contains(lowerSearchTerm)))
                    .OrderBy(t => t.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search templates with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<int> GetUsageCountAsync(int templateId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.EmailQueue
                    .Where(e => e.TemplateId == templateId &&
                               e.CreatedAt >= fromDate &&
                               e.CreatedAt <= toDate)
                    .CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get usage count for template {TemplateId}", templateId);
                throw;
            }
        }
    }
}