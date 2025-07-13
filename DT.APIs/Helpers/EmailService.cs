using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using DT.APIs.Models;

namespace DT.APIs.Helpers
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, IWebHostEnvironment env, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _env = env;
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailModel emailModel)
        {
            // Normalize and validate the email model
            NormalizeEmailModel(emailModel);
            ValidateEmailModel(emailModel);

            var mailSettings = _configuration.GetSection("MailSettings");

            using var smtpClient = new SmtpClient(mailSettings["Server"], int.Parse(mailSettings["Port"]));
            using var mailMessage = CreateMailMessage(emailModel, mailSettings);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {RecipientEmail}", emailModel.RecipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {RecipientEmail}", emailModel.RecipientEmail);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task<BulkEmailResultModel> SendBulkEmailAsync(BulkEmailModel bulkEmailModel)
        {
            // Normalize the bulk email model
            NormalizeBulkEmailModel(bulkEmailModel);

            var startTime = DateTime.UtcNow;
            var result = new BulkEmailResultModel
            {
                TotalEmails = bulkEmailModel.Recipients?.Count ?? 0
            };

            if (result.TotalEmails == 0)
            {
                throw new ArgumentException("Recipients list cannot be empty");
            }

            if (bulkEmailModel.SendIndividually)
            {
                await SendIndividualEmails(bulkEmailModel, result);
            }
            else
            {
                await SendSingleEmailToMultipleRecipients(bulkEmailModel, result);
            }

            result.TotalProcessingTime = DateTime.UtcNow - startTime;
            return result;
        }

        private void NormalizeEmailModel(EmailModel emailModel)
        {
            if (emailModel == null) return;

            // Initialize collections if null
            emailModel.CC ??= new List<string>();
            emailModel.BCC ??= new List<string>();
            emailModel.Attachments ??= new List<EmailAttachment>();
            emailModel.CustomHeaders ??= new Dictionary<string, string>();

            // Remove null, empty, or invalid emails from CC and BCC
            emailModel.CC = emailModel.CC.Where(email => !string.IsNullOrWhiteSpace(email) && IsValidEmail(email)).ToList();
            emailModel.BCC = emailModel.BCC.Where(email => !string.IsNullOrWhiteSpace(email) && IsValidEmail(email)).ToList();

            // Set default ReplyTo if not provided or invalid
            if (string.IsNullOrWhiteSpace(emailModel.ReplyTo) || !IsValidEmail(emailModel.ReplyTo))
            {
                var mailSettings = _configuration.GetSection("MailSettings");
                emailModel.ReplyTo = mailSettings["SenderEmail"] ?? "noreply@stc.com.sa";
            }

            // Handle attachments - remove null/invalid ones and set defaults for valid ones
            if (emailModel.Attachments?.Any() == true)
            {
                var validAttachments = new List<EmailAttachment>();

                foreach (var attachment in emailModel.Attachments.Where(att => att != null))
                {
                    // Skip attachments with missing critical data
                    if (string.IsNullOrWhiteSpace(attachment.Content))
                    {
                        _logger.LogWarning("Skipping attachment with empty content: {FileName}", attachment.FileName ?? "unknown");
                        continue;
                    }

                    // Set default values for missing properties
                    attachment.FileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "attachment" : attachment.FileName;
                    attachment.ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType;
                    attachment.ContentId = string.IsNullOrWhiteSpace(attachment.ContentId) ? Guid.NewGuid().ToString() : attachment.ContentId;

                    // Validate base64 content
                    try
                    {
                        Convert.FromBase64String(attachment.Content);
                        validAttachments.Add(attachment);
                    }
                    catch (FormatException)
                    {
                        _logger.LogWarning("Skipping attachment with invalid base64 content: {FileName}", attachment.FileName);
                    }
                }

                emailModel.Attachments = validAttachments;
            }

            // Handle custom headers - remove null/empty ones
            if (emailModel.CustomHeaders?.Any() == true)
            {
                var validHeaders = emailModel.CustomHeaders
                    .Where(h => !string.IsNullOrWhiteSpace(h.Key) && h.Value != null)
                    .ToDictionary(h => h.Key, h => h.Value ?? string.Empty);
                emailModel.CustomHeaders = validHeaders;
            }

            // Ensure subject and body are not null (set defaults if needed)
            emailModel.Subject ??= "No Subject";
            emailModel.Body ??= string.Empty;
        }

        private void NormalizeBulkEmailModel(BulkEmailModel bulkEmailModel)
        {
            if (bulkEmailModel == null) return;

            // Initialize collections if null
            bulkEmailModel.Recipients ??= new List<string>();
            bulkEmailModel.CC ??= new List<string>();
            bulkEmailModel.BCC ??= new List<string>();
            bulkEmailModel.Attachments ??= new List<EmailAttachment>();

            // Remove null, empty, or invalid emails
            bulkEmailModel.Recipients = bulkEmailModel.Recipients.Where(email => !string.IsNullOrWhiteSpace(email) && IsValidEmail(email)).ToList();
            bulkEmailModel.CC = bulkEmailModel.CC.Where(email => !string.IsNullOrWhiteSpace(email) && IsValidEmail(email)).ToList();
            bulkEmailModel.BCC = bulkEmailModel.BCC.Where(email => !string.IsNullOrWhiteSpace(email) && IsValidEmail(email)).ToList();

            // Set default ReplyTo if not provided or invalid
            if (string.IsNullOrWhiteSpace(bulkEmailModel.ReplyTo) || !IsValidEmail(bulkEmailModel.ReplyTo))
            {
                var mailSettings = _configuration.GetSection("MailSettings");
                bulkEmailModel.ReplyTo = mailSettings["SenderEmail"] ?? "noreply@stc.com.sa";
            }

            // Handle attachments - same logic as regular email
            if (bulkEmailModel.Attachments?.Any() == true)
            {
                var validAttachments = new List<EmailAttachment>();

                foreach (var attachment in bulkEmailModel.Attachments.Where(att => att != null))
                {
                    // Skip attachments with missing critical data
                    if (string.IsNullOrWhiteSpace(attachment.Content))
                    {
                        _logger.LogWarning("Skipping bulk email attachment with empty content: {FileName}", attachment.FileName ?? "unknown");
                        continue;
                    }

                    // Set default values for missing properties
                    attachment.FileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "attachment" : attachment.FileName;
                    attachment.ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType;
                    attachment.ContentId = string.IsNullOrWhiteSpace(attachment.ContentId) ? Guid.NewGuid().ToString() : attachment.ContentId;

                    // Validate base64 content
                    try
                    {
                        Convert.FromBase64String(attachment.Content);
                        validAttachments.Add(attachment);
                    }
                    catch (FormatException)
                    {
                        _logger.LogWarning("Skipping bulk email attachment with invalid base64 content: {FileName}", attachment.FileName);
                    }
                }

                bulkEmailModel.Attachments = validAttachments;
            }

            // Ensure subject and body are not null
            bulkEmailModel.Subject ??= "No Subject";
            bulkEmailModel.Body ??= string.Empty;
        }

        private async Task SendIndividualEmails(BulkEmailModel bulkEmailModel, BulkEmailResultModel result)
        {
            var successCounter = 0;
            var failureCounter = 0;

            var tasks = bulkEmailModel.Recipients.Select(async recipient =>
            {
                var emailResult = new EmailSendResult
                {
                    RecipientEmail = recipient,
                    SentAt = DateTime.UtcNow
                };

                try
                {
                    var emailModel = new EmailModel
                    {
                        Subject = bulkEmailModel.Subject,
                        RecipientEmail = recipient,
                        Body = bulkEmailModel.Body,
                        CC = new List<string>(bulkEmailModel.CC ?? new List<string>()),
                        BCC = new List<string>(bulkEmailModel.BCC ?? new List<string>()),
                        ReplyTo = bulkEmailModel.ReplyTo,
                        Priority = bulkEmailModel.Priority,
                        IsBodyHtml = bulkEmailModel.IsBodyHtml,
                        Attachments = new List<EmailAttachment>(bulkEmailModel.Attachments ?? new List<EmailAttachment>())
                    };

                    await SendEmailAsync(emailModel);
                    emailResult.IsSuccess = true;
                    Interlocked.Increment(ref successCounter);
                }
                catch (Exception ex)
                {
                    emailResult.IsSuccess = false;
                    emailResult.ErrorMessage = ex.Message;
                    Interlocked.Increment(ref failureCounter);
                    _logger.LogError(ex, "Failed to send bulk email to {Recipient}", recipient);
                }

                lock (result.Results)
                {
                    result.Results.Add(emailResult);
                }
            });

            await Task.WhenAll(tasks);

            // Update the result counters
            result.SuccessfulSends = successCounter;
            result.FailedSends = failureCounter;
        }

        private async Task SendSingleEmailToMultipleRecipients(BulkEmailModel bulkEmailModel, BulkEmailResultModel result)
        {
            try
            {
                var emailModel = new EmailModel
                {
                    Subject = bulkEmailModel.Subject,
                    RecipientEmail = bulkEmailModel.Recipients.First(),
                    Body = bulkEmailModel.Body,
                    CC = bulkEmailModel.Recipients.Skip(1).Concat(bulkEmailModel.CC ?? new List<string>()).ToList(),
                    BCC = new List<string>(bulkEmailModel.BCC ?? new List<string>()),
                    ReplyTo = bulkEmailModel.ReplyTo,
                    Priority = bulkEmailModel.Priority,
                    IsBodyHtml = bulkEmailModel.IsBodyHtml,
                    Attachments = new List<EmailAttachment>(bulkEmailModel.Attachments ?? new List<EmailAttachment>())
                };

                await SendEmailAsync(emailModel);

                result.SuccessfulSends = bulkEmailModel.Recipients.Count;
                result.Results.AddRange(bulkEmailModel.Recipients.Select(r => new EmailSendResult
                {
                    RecipientEmail = r,
                    IsSuccess = true,
                    SentAt = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                result.FailedSends = bulkEmailModel.Recipients.Count;
                result.Results.AddRange(bulkEmailModel.Recipients.Select(r => new EmailSendResult
                {
                    RecipientEmail = r,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    SentAt = DateTime.UtcNow
                }));
                _logger.LogError(ex, "Failed to send bulk email to multiple recipients");
            }
        }

        private MailMessage CreateMailMessage(EmailModel emailModel, IConfigurationSection mailSettings)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(mailSettings["SenderEmail"], mailSettings["SenderName"]),
                Subject = emailModel.Subject ?? string.Empty,
                Body = emailModel.Body ?? string.Empty,
                IsBodyHtml = emailModel.IsBodyHtml,
                Priority = emailModel.Priority
            };

            // Add primary recipient
            mailMessage.To.Add(new MailAddress(emailModel.RecipientEmail, emailModel.RecipientName ?? string.Empty));

            // Add CC recipients (only valid ones)
            if (emailModel.CC?.Any() == true)
            {
                foreach (var cc in emailModel.CC.Where(IsValidEmail))
                {
                    try
                    {
                        mailMessage.CC.Add(cc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add CC recipient: {CC}", cc);
                    }
                }
            }

            // Add BCC recipients (only valid ones)
            if (emailModel.BCC?.Any() == true)
            {
                foreach (var bcc in emailModel.BCC.Where(IsValidEmail))
                {
                    try
                    {
                        mailMessage.Bcc.Add(bcc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add BCC recipient: {BCC}", bcc);
                    }
                }
            }

            // Set reply-to (already normalized)
            if (!string.IsNullOrWhiteSpace(emailModel.ReplyTo) && IsValidEmail(emailModel.ReplyTo))
            {
                mailMessage.ReplyToList.Add(emailModel.ReplyTo);
            }

            // Add custom headers (only valid ones)
            if (emailModel.CustomHeaders?.Any() == true)
            {
                foreach (var header in emailModel.CustomHeaders)
                {
                    try
                    {
                        mailMessage.Headers.Add(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add custom header: {Key}={Value}", header.Key, header.Value);
                    }
                }
            }

            // Add delivery and read receipts
            if (emailModel.RequestDeliveryNotification)
            {
                mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.OnFailure;
            }

            if (emailModel.RequestReadReceipt)
            {
                mailMessage.Headers.Add("Disposition-Notification-To", mailSettings["SenderEmail"]);
            }

            // Handle attachments and inline content
            if (emailModel.Attachments?.Any() == true)
            {
                HandleAttachments(mailMessage, emailModel);
            }
            else
            {
                // Add default template images if no custom attachments
                AddDefaultTemplateImages(mailMessage, emailModel.Body);
            }

            return mailMessage;
        }

        private void HandleAttachments(MailMessage mailMessage, EmailModel emailModel)
        {
            AlternateView? htmlView = null;

            if (emailModel.IsBodyHtml)
            {
                htmlView = AlternateView.CreateAlternateViewFromString(emailModel.Body, null, MediaTypeNames.Text.Html);
                mailMessage.AlternateViews.Add(htmlView);
            }

            foreach (var attachment in emailModel.Attachments)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(attachment.Content))
                    {
                        _logger.LogWarning("Skipping attachment with empty content: {FileName}", attachment.FileName);
                        continue;
                    }

                    var fileBytes = Convert.FromBase64String(attachment.Content);
                    var memoryStream = new MemoryStream(fileBytes);

                    if (attachment.IsInline && htmlView != null)
                    {
                        // Inline attachment (embedded in HTML)
                        var linkedResource = new LinkedResource(memoryStream)
                        {
                            ContentId = attachment.ContentId ?? Guid.NewGuid().ToString(),
                            ContentType = new ContentType(attachment.ContentType ?? "application/octet-stream")
                        };
                        htmlView.LinkedResources.Add(linkedResource);
                    }
                    else
                    {
                        // Regular attachment
                        var mailAttachment = new Attachment(memoryStream, attachment.FileName ?? "attachment")
                        {
                            ContentType = new ContentType(attachment.ContentType ?? "application/octet-stream")
                        };
                        mailMessage.Attachments.Add(mailAttachment);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process attachment {FileName}", attachment.FileName);
                }
            }
        }

        private void AddDefaultTemplateImages(MailMessage mailMessage, string body)
        {
            try
            {
                var logoPath = Path.Combine(_env.WebRootPath ?? string.Empty, "site/images/mail_logo.png");
                var headerPath = Path.Combine(_env.WebRootPath ?? string.Empty, "site/images/mail_header.png");

                if (!File.Exists(logoPath) && !File.Exists(headerPath))
                    return;

                var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

                if (File.Exists(logoPath))
                {
                    var logoImage = new LinkedResource(logoPath)
                    {
                        ContentId = "mail_img1",
                        ContentType = new ContentType("image/png")
                    };
                    htmlView.LinkedResources.Add(logoImage);
                }

                if (File.Exists(headerPath))
                {
                    var headerImage = new LinkedResource(headerPath)
                    {
                        ContentId = "mail_img2",
                        ContentType = new ContentType("image/png")
                    };
                    htmlView.LinkedResources.Add(headerImage);
                }

                mailMessage.AlternateViews.Add(htmlView);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add default template images");
            }
        }

        private void ValidateEmailModel(EmailModel emailModel)
        {
            if (emailModel == null)
                throw new ArgumentNullException(nameof(emailModel), "Email model cannot be null.");

            if (string.IsNullOrWhiteSpace(emailModel.RecipientEmail))
                throw new ArgumentException("Recipient email cannot be null or empty.", nameof(emailModel.RecipientEmail));

            if (!IsValidEmail(emailModel.RecipientEmail))
                throw new ArgumentException("Invalid recipient email format.", nameof(emailModel.RecipientEmail));

            if (string.IsNullOrWhiteSpace(emailModel.Subject))
                throw new ArgumentException("Subject cannot be null or empty.", nameof(emailModel.Subject));

            if (string.IsNullOrWhiteSpace(emailModel.Body))
                throw new ArgumentException("Body cannot be null or empty.", nameof(emailModel.Body));
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public string ProcessTemplate(string template, Dictionary<string, string>? placeholders)
        {
            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            if (placeholders == null || !placeholders.Any())
                return template;

            var result = template;
            foreach (var placeholder in placeholders.Where(p => !string.IsNullOrWhiteSpace(p.Key)))
            {
                result = result.Replace($"{{{placeholder.Key}}}", placeholder.Value ?? string.Empty);
            }
            return result;
        }
    }
}