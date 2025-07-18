using DT.EmailWorker.Core.Utilities;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

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
            var result = new EmailProcessingResult();

            try
            {
                _logger.LogDebug("Starting email processing for {QueueId}", request.QueueId);

                // Validate email
                var validation = await ValidateEmailAsync(request);
                if (!validation.IsValid)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = string.Join(", ", validation.Errors);
                    result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                // Process template if specified
                if (request.RequiresTemplateProcessing && request.TemplateId.HasValue)
                {
                    var templateResult = await ProcessTemplateAsync(request, cancellationToken);
                    if (!templateResult.IsSuccess)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = templateResult.ErrorMessage;
                        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                        return result;
                    }
                }

                // Process attachments if any
                if (!string.IsNullOrWhiteSpace(request.Attachments))
                {
                    var attachmentResult = await ProcessAttachmentsAsync(request.Attachments);
                    if (!attachmentResult.IsSuccess)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = attachmentResult.ErrorMessage;
                        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                        return result;
                    }
                }

                // Send email via SMTP - FIXED: Handle the bool return type
                var sendSuccess = await _smtpService.SendEmailAsync(request);

                stopwatch.Stop();

                result.IsSuccess = sendSuccess;
                result.ErrorMessage = sendSuccess ? null : "SMTP send failed";
                result.MessageId = sendSuccess ? Guid.NewGuid().ToString() : null; // Generate a message ID if successful
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Email {QueueId} processed successfully in {ProcessingTime}ms",
                        request.QueueId, result.ProcessingTimeMs);
                }
                else
                {
                    _logger.LogWarning("Email {QueueId} processing failed: {Error}",
                        request.QueueId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Error processing email {QueueId}", request.QueueId);

                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
        }

        public async Task<EmailProcessingResult> ProcessTemplateEmailAsync(TemplateEmailRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Processing template email with template {TemplateName}", request.TemplateName);

                // Get template and process
                var templateResult = await _templateService.ProcessTemplateByNameAsync(
                    request.TemplateName,
                    request.TemplateData,
                    cancellationToken);

                if (!templateResult.IsSuccess)
                {
                    return new EmailProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = templateResult.ErrorMessage
                    };
                }

                // Create processing request
                var processingRequest = new EmailProcessingRequest
                {
                    QueueId = Guid.NewGuid(),
                    ToEmails = request.ToEmails,
                    CcEmails = request.CcEmails,
                    BccEmails = request.BccEmails,
                    Subject = templateResult.ProcessedSubject,
                    Body = templateResult.ProcessedBody,
                    IsHtml = true,
                    Attachments = JsonSerializer.Serialize(request.Attachments ?? new List<AttachmentData>()),
                    CreatedBy = "TemplateProcessor",
                    RequestSource = "Template"
                };

                // Process the email
                return await ProcessEmailAsync(processingRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template email {TemplateName}", request.TemplateName);
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

            try
            {
                // Validate recipients
                if (string.IsNullOrWhiteSpace(request.ToEmails))
                {
                    result.IsValid = false;
                    result.Errors.Add("At least one recipient email is required");
                }
                else
                {
                    var toValidation = EmailValidator.ValidateEmails(request.ToEmails);
                    if (toValidation.Any(v => !v.IsValid))
                    {
                        result.IsValid = false;
                        result.Errors.AddRange(toValidation.Where(v => !v.IsValid).Select(v => $"Invalid recipient email: {v.ErrorMessage}"));
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

                // Validate attachments if any
                if (!string.IsNullOrWhiteSpace(request.Attachments))
                {
                    try
                    {
                        var attachments = JsonSerializer.Deserialize<List<AttachmentData>>(request.Attachments);
                        if (attachments != null)
                        {
                            foreach (var attachment in attachments)
                            {
                                if (string.IsNullOrWhiteSpace(attachment.FileName))
                                {
                                    result.Warnings.Add("Attachment has no filename");
                                }

                                if (string.IsNullOrWhiteSpace(attachment.Content) && string.IsNullOrWhiteSpace(attachment.FilePath))
                                {
                                    result.Warnings.Add($"Attachment {attachment.FileName} has no content or file path");
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        result.Warnings.Add("Invalid attachment data format");
                    }
                }

                await Task.CompletedTask; // Make async for future enhancements
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email");
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
                return result;
            }
        }

        private async Task<EmailProcessingResult> ProcessTemplateAsync(EmailProcessingRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!request.TemplateId.HasValue)
                {
                    return new EmailProcessingResult { IsSuccess = true };
                }

                // Parse template data from JSON string
                var templateData = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(request.TemplateData))
                {
                    try
                    {
                        templateData = JsonSerializer.Deserialize<Dictionary<string, string>>(request.TemplateData)
                                     ?? new Dictionary<string, string>();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse template data for request {QueueId}", request.QueueId);
                        return new EmailProcessingResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Invalid template data format"
                        };
                    }
                }

                var result = await _templateService.ProcessTemplateAsync(request.TemplateId.Value, templateData, cancellationToken);

                if (result.IsSuccess)
                {
                    // Update the request with processed content
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
                _logger.LogError(ex, "Error processing template for request {QueueId}", request.QueueId);
                return new EmailProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }

        private async Task<EmailProcessingResult> ProcessAttachmentsAsync(string attachmentsJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(attachmentsJson))
                {
                    return new EmailProcessingResult { IsSuccess = true };
                }

                var attachments = JsonSerializer.Deserialize<List<AttachmentData>>(attachmentsJson);
                if (attachments == null || !attachments.Any())
                {
                    return new EmailProcessingResult { IsSuccess = true };
                }

                // Convert AttachmentData to EmailAttachment entities
                var emailAttachments = attachments.Select(a => new Models.Entities.EmailAttachment
                {
                    FileName = a.FileName,
                    ContentType = a.ContentType ?? "application/octet-stream",
                    Content = a.Content,
                    FilePath = a.FilePath,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList();

                var result = await _attachmentProcessor.ProcessAttachmentsAsync(emailAttachments);

                return new EmailProcessingResult
                {
                    IsSuccess = result.IsSuccess,
                    ErrorMessage = result.HasErrors ? "One or more attachment processing errors occurred" : null
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing attachment JSON");
                return new EmailProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid attachment data format"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing attachments");
                return new EmailProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }

        private async Task<List<AttachmentData>> ParseAttachmentsFromJsonAsync(string? attachmentsJson)
        {
            await Task.CompletedTask; // Make async

            if (string.IsNullOrWhiteSpace(attachmentsJson))
            {
                return new List<AttachmentData>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<AttachmentData>>(attachmentsJson) ?? new List<AttachmentData>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse attachments JSON");
                return new List<AttachmentData>();
            }
        }
    }
}