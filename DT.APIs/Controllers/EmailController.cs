using DT.APIs.Helpers;
using DT.APIs.Models;
using DT.APIs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT.APIs.Controllers
{
    [Authorize]
    [ApiController]
    [Route("email")]
    public class EmailController : ControllerBase

    {
        private readonly EmailService _emailService;
        private readonly IEmailQueueService _emailQueueService;
        private readonly ILogger<EmailController> _logger;
        private readonly IConfiguration _configuration;

        public EmailController(IEmailQueueService emailQueueService, IConfiguration configuration,EmailService EmailService, ILogger<EmailController> logger)
        {
            _emailService = EmailService;
            _emailQueueService = emailQueueService;
            _logger = logger;
            _configuration = configuration;
        }


        #region SMTP Email APIs

        [HttpPost("send-email")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmail([FromBody] EmailModel emailModel)
        {
            try
            {
                // Basic null check
                if (emailModel == null)
                {
                    return BadRequest("Email model cannot be null");
                }

                await _emailService.SendEmailAsync(emailModel);
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {RecipientEmail}", emailModel?.RecipientEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send email");
            }
        }

        [HttpPost("send-bulk-email")]
        [ProducesResponseType(typeof(BulkEmailResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendBulkEmail([FromBody] BulkEmailModel bulkEmailModel)
        {
            try
            {
                // Basic null check
                if (bulkEmailModel == null)
                {
                    return BadRequest("Bulk email model cannot be null");
                }

                var result = await _emailService.SendBulkEmailAsync(bulkEmailModel);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid bulk email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk email");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send bulk email");
            }
        }

        [HttpPost("send-template-email")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendTemplateEmail([FromBody] EmailTemplateModel templateModel, [FromQuery] string recipientEmail)
        {
            try
            {
                if (templateModel == null)
                {
                    return BadRequest("Template model cannot be null");
                }

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    return BadRequest("Recipient email is required");
                }

                // Handle null placeholders gracefully
                templateModel.Placeholders ??= new Dictionary<string, string>();
                templateModel.Subject ??= "No Subject";
                templateModel.Body ??= string.Empty;

                var processedSubject = _emailService.ProcessTemplate(templateModel.Subject, templateModel.Placeholders);
                var processedBody = _emailService.ProcessTemplate(templateModel.Body, templateModel.Placeholders);

                var emailModel = new EmailModel
                {
                    Subject = processedSubject,
                    RecipientEmail = recipientEmail.Trim(),
                    Body = processedBody,
                    IsBodyHtml = true
                    // All other properties will be handled by NormalizeEmailModel
                };

                await _emailService.SendEmailAsync(emailModel);
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid template email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending template email to {RecipientEmail}", recipientEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send template email");
            }
        }

        [HttpPost("send-email-with-attachments")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmailWithAttachments([FromBody] EmailModel emailModel)
        {
            try
            {
                if (emailModel == null)
                {
                    return BadRequest("Email model cannot be null");
                }

                // Check attachment size limits (e.g., 25MB total) - only if attachments exist
                const int maxTotalSizeMB = 25;
                const int maxTotalSizeBytes = maxTotalSizeMB * 1024 * 1024;

                if (emailModel.Attachments?.Any() == true)
                {
                    var totalSize = 0L;
                    foreach (var attachment in emailModel.Attachments)
                    {
                        if (!string.IsNullOrWhiteSpace(attachment?.Content))
                        {
                            try
                            {
                                totalSize += Convert.FromBase64String(attachment.Content).Length;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Invalid attachment content for {FileName}", attachment.FileName);
                                // Continue with other attachments
                            }
                        }
                    }

                    if (totalSize > maxTotalSizeBytes)
                    {
                        return StatusCode(StatusCodes.Status413RequestEntityTooLarge,
                            $"Total attachment size exceeds {maxTotalSizeMB}MB limit");
                    }
                }

                await _emailService.SendEmailAsync(emailModel);
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email with attachments to {RecipientEmail}", emailModel?.RecipientEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send email with attachments");
            }
        }

        [HttpGet("test-email-connection")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TestEmailConnection()
        {
            try
            {
                var mailSettings = _configuration.GetSection("MailSettings");

                var testEmail = new EmailModel
                {
                    Subject = "Email Service Test",
                    RecipientEmail = mailSettings["SenderEmail"] ?? "test@example.com",
                    Body = "<h1>Email Service Test</h1><p>This is a test email to verify the email service is working correctly.</p>",
                    IsBodyHtml = true
                    // Intentionally leaving other properties null to test normalization
                };

                await _emailService.SendEmailAsync(testEmail);

                return Ok(new
                {
                    success = true,
                    message = "Email service is working correctly",
                    server = mailSettings["Server"],
                    port = mailSettings["Port"],
                    senderEmail = mailSettings["SenderEmail"],
                    testSentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email connection test failed");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Email service connection failed",
                    error = ex.Message
                });
            }
        }

        [HttpPost("send-email-flexible-test")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmailFlexibleTest([FromBody] EmailModel emailModel)
        {
            try
            {
                if (emailModel == null)
                {
                    return BadRequest("Email model cannot be null");
                }

                _logger.LogInformation("Testing flexible email handling with input: {@EmailModel}", new
                {
                    emailModel.Subject,
                    emailModel.RecipientEmail,
                    emailModel.ReplyTo,
                    CCCount = emailModel.CC?.Count ?? 0,
                    BCCCount = emailModel.BCC?.Count ?? 0,
                    AttachmentsCount = emailModel.Attachments?.Count ?? 0,
                    CustomHeadersCount = emailModel.CustomHeaders?.Count ?? 0
                });

                await _emailService.SendEmailAsync(emailModel);

                return Ok(new
                {
                    success = true,
                    message = "Email sent successfully with flexible handling",
                    processedAt = DateTime.UtcNow,
                    originalReplyTo = emailModel.ReplyTo,
                    finalCCCount = emailModel.CC?.Count ?? 0,
                    finalBCCCount = emailModel.BCC?.Count ?? 0,
                    finalAttachmentsCount = emailModel.Attachments?.Count ?? 0
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid email data provided in flexible test");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in flexible email test");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send test email");
            }
        }

        #endregion

        #region Queue Email APIs


        /// <summary>
        /// Queue a regular email for processing
        /// </summary>
        [HttpPost("queue")]
        [ProducesResponseType(typeof(QueueEmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueueEmail([FromBody] QueueEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _emailQueueService.QueueEmailAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing email");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Queue a template-based email for processing
        /// </summary>
        [HttpPost("queue-template")]
        [ProducesResponseType(typeof(QueueEmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueueTemplateEmail([FromBody] QueueTemplateEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _emailQueueService.QueueTemplateEmailAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing template email");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Queue multiple emails in bulk
        /// </summary>
        [HttpPost("queue-bulk")]
        [ProducesResponseType(typeof(BulkQueueEmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueueBulkEmail([FromBody] QueueBulkEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _emailQueueService.QueueBulkEmailAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing bulk email");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get status of a specific queued email
        /// </summary>
        [HttpGet("status/{queueId}")]
        [ProducesResponseType(typeof(EmailStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmailStatus(Guid queueId)
        {
            try
            {
                var result = await _emailQueueService.GetEmailStatusAsync(queueId);
                if (result == null)
                {
                    return NotFound($"Email with ID {queueId} not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email status for {QueueId}", queueId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get status of multiple queued emails
        /// </summary>
        [HttpPost("status/batch")]
        [ProducesResponseType(typeof(List<EmailStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBatchEmailStatus([FromBody] List<Guid> queueIds)
        {
            try
            {
                if (queueIds == null || !queueIds.Any())
                {
                    return BadRequest("Queue IDs list cannot be empty");
                }

                var result = await _emailQueueService.GetBatchEmailStatusAsync(queueIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch email status");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Cancel a queued email
        /// </summary>
        [HttpPost("cancel/{queueId}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelEmail(Guid queueId)
        {
            try
            {
                var result = await _emailQueueService.CancelEmailAsync(queueId);
                if (!result)
                {
                    return NotFound($"Email with ID {queueId} not found or cannot be cancelled");
                }

                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling email {QueueId}", queueId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get email queue health status
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(QueueHealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueueHealth()
        {
            try
            {
                var result = await _emailQueueService.GetQueueHealthAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue health");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get email queue statistics
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(QueueStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueueStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var result = await _emailQueueService.GetQueueStatisticsAsync(fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get paginated list of queued emails
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(PagedEmailQueueResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueuedEmails(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? search = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var result = await _emailQueueService.GetQueuedEmailsAsync(page, pageSize, status, priority, fromDate, toDate, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queued emails");
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion
    }
}