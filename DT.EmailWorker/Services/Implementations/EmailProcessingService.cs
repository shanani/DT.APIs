using DT.EmailWorker.Core.Utilities;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// Core email processing service implementation
    /// </summary>
    public class EmailProcessingService : IEmailProcessingService
    {
        private readonly ISmtpService _smtpService;
        private readonly ITemplateService _templateService;
        private readonly AttachmentProcessor _attachmentProcessor;
        private readonly ILogger<EmailProcessingService> _logger;

        public EmailProcessingService(
            ISmtpService smtpService,
            ITemplateService templateService,
            AttachmentProcessor attachmentProcessor,
            ILogger<EmailProcessingService> logger)
        {
            _smtpService = smtpService;
            _templateService = templateService;
            _attachmentProcessor = attachmentProcessor;
            _logger = logger;
        }

        public async Task<EmailProcessingResult> ProcessEmailAsync(EmailProcessingRequest request, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate email
                var validation = await ValidateEmailAsync(request);
                if (!validation.IsValid)
                {
                    return new EmailProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = string.Join(", ", validation.Errors),
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // Process template if specified
                if (request.TemplateId.HasValue)
                {
                    var templateResult = await ProcessTemplateAsync(request, cancellationToken);
                    if (!templateResult.IsSuccess)
                    {
                        return templateResult;
                    }
                }

                // Process attachments
                if (request.Attachments?.Any() == true)
                {
                    var attachmentResult = await ProcessAttachmentsAsync(request.Attachments);
                    if (!attachmentResult.IsSuccess)
                    {
                        return new EmailProcessingResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Attachment processing failed: {attachmentResult.ErrorMessage}",
                            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                        };
                    }
                }

                // Send email via SMTP
                var success = await _smtpService.SendEmailAsync(request);

                stopwatch.Stop();

                return new EmailProcessingResult
                {
                    IsSuccess = success,
                    ErrorMessage = success ? null : "Failed to send email via SMTP",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    MessageId = success ? Guid.NewGuid().ToString() : null
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error processing email");

                return new EmailProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        public async Task<EmailProcessingResult> ProcessTemplateEmailAsync(TemplateEmailRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get template
                var template = await _templateService.GetTemplateByNameAsync(request.TemplateName, cancellationToken);
                if (template == null)
                {
                    return new EmailProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Template '{request.TemplateName}' not found"
                    };
                }

                // Process template
                var processedTemplate = await _templateService.ProcessTemplateAsync(template.Id, request.TemplateData, cancellationToken);
                if (!processedTemplate.IsSuccess)
                {
                    return new EmailProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Template processing failed: {processedTemplate.ErrorMessage}"
                    };
                }

                // Create processing request
                var processingRequest = new EmailProcessingRequest
                {
                    ToEmails = request.ToEmails,
                    CcEmails = request.CcEmails,
                    BccEmails = request.BccEmails,
                    Subject = processedTemplate.ProcessedSubject,
                    Body = processedTemplate.ProcessedBody,
                    IsHtml = true,
                    Attachments = request.Attachments,
                    TemplateId = template.Id
                };

                return await ProcessEmailAsync(processingRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template email");
                return new EmailProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }

        public async Task<ValidationResult> ValidateEmailAsync(EmailProcessingRequest request)
        {
            var result = new ValidationResult { IsValid = true };

            // Validate recipients
            if (string.IsNullOrWhiteSpace(request.ToEmails))
            {
                result.IsValid = false;
                result.Errors.Add("To emails are required");
            }
            else
            {
                var emailValidation = EmailValidator.ValidateEmails(request.ToEmails);
                if (emailValidation.Any(v => !v.IsValid))
                {
                    result.IsValid = false;
                    result.Errors.AddRange(emailValidation.Where(v => !v.IsValid).Select(v => v.ErrorMessage ?? "Invalid email"));
                }
            }

            // Validate subject
            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                result.IsValid = false;
                result.Errors.Add("Subject is required");
            }

            // Validate body
            if (string.IsNullOrWhiteSpace(request.Body))
            {
                result.IsValid = false;
                result.Errors.Add("Body is required");
            }

            // Validate CC emails if provided
            if (!string.IsNullOrWhiteSpace(request.CcEmails))
            {
                var ccValidation = EmailValidator.ValidateEmails(request.CcEmails);
                if (ccValidation.Any(v => !v.IsValid))
                {
                    result.Warnings.AddRange(ccValidation.Where(v => !v.IsValid).Select(v => $"Invalid CC email: {v.ErrorMessage}"));
                }
            }

            // Validate BCC emails if provided
            if (!string.IsNullOrWhiteSpace(request.BccEmails))
            {
                var bccValidation = EmailValidator.ValidateEmails(request.BccEmails);
                if (bccValidation.Any(v => !v.IsValid))
                {
                    result.Warnings.AddRange(bccValidation.Where(v => !v.IsValid).Select(v => $"Invalid BCC email: {v.ErrorMessage}"));
                }
            }

            await Task.CompletedTask; // Make async for future enhancements
            return result;
        }

        private async Task<EmailProcessingResult> ProcessTemplateAsync(EmailProcessingRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!request.TemplateId.HasValue)
                {
                    return new EmailProcessingResult { IsSuccess = true };
                }

                var templateData = string.IsNullOrEmpty(request.TemplateData)
                    ? new Dictionary<string, string>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(request.TemplateData) ?? new Dictionary<string, string>();

                var result = await _templateService.ProcessTemplateAsync(request.TemplateId.Value, templateData, cancellationToken);

                if (result.IsSuccess)
                {
                    request.Subject = result.ProcessedSubject;
                    request.Body = result.ProcessedBody;
                }

                return new EmailProcessingResult
                {
                    IsSuccess = result.IsSuccess,
                    ErrorMessage = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                return new EmailProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }

        private async Task<EmailProcessingResult> ProcessAttachmentsAsync(List<AttachmentData> attachments)
        {
            try
            {
                var emailAttachments = attachments.Select(a => new Models.Entities.EmailAttachment
                {
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    Content = a.Content,
                    FilePath = a.FilePath
                }).ToList();

                var result = await _attachmentProcessor.ProcessAttachmentsAsync(emailAttachments);

                return new EmailProcessingResult
                {
                    IsSuccess = result.IsSuccess,
                    ErrorMessage = result.HasErrors ? "Attachment processing errors occurred" : null
                };
            }
            catch (Exception ex)
            {
                return new EmailProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }
    }
}