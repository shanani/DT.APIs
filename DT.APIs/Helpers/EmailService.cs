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
            var startTime = DateTime.UtcNow;
            var result = new BulkEmailResultModel
            {
                TotalEmails = bulkEmailModel.Recipients.Count
            };

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

        private async Task SendIndividualEmails(BulkEmailModel bulkEmailModel, BulkEmailResultModel result)
        {
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
                        CC = bulkEmailModel.CC,
                        BCC = bulkEmailModel.BCC,
                        Priority = bulkEmailModel.Priority,
                        IsBodyHtml = bulkEmailModel.IsBodyHtml,
                        Attachments = bulkEmailModel.Attachments
                    };

                    await SendEmailAsync(emailModel);
                    emailResult.IsSuccess = true;
                    result.SuccessfulSends++;
                }
                catch (Exception ex)
                {
                    emailResult.IsSuccess = false;
                    emailResult.ErrorMessage = ex.Message;
                    result.FailedSends++;
                    _logger.LogError(ex, "Failed to send bulk email to {Recipient}", recipient);
                }

                lock (result.Results)
                {
                    result.Results.Add(emailResult);
                }
            });

            await Task.WhenAll(tasks);
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
                    BCC = bulkEmailModel.BCC,
                    Priority = bulkEmailModel.Priority,
                    IsBodyHtml = bulkEmailModel.IsBodyHtml,
                    Attachments = bulkEmailModel.Attachments
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
            }
        }

        private MailMessage CreateMailMessage(EmailModel emailModel, IConfigurationSection mailSettings)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(mailSettings["SenderEmail"], mailSettings["SenderName"]),
                Subject = emailModel.Subject,
                Body = emailModel.Body,
                IsBodyHtml = emailModel.IsBodyHtml,
                Priority = emailModel.Priority
            };

            // Add primary recipient
            mailMessage.To.Add(new MailAddress(emailModel.RecipientEmail, emailModel.RecipientName ?? ""));

            // Add CC recipients
            if (emailModel.CC?.Any() == true)
            {
                foreach (var cc in emailModel.CC)
                {
                    if (IsValidEmail(cc))
                        mailMessage.CC.Add(cc);
                }
            }

            // Add BCC recipients
            if (emailModel.BCC?.Any() == true)
            {
                foreach (var bcc in emailModel.BCC)
                {
                    if (IsValidEmail(bcc))
                        mailMessage.Bcc.Add(bcc);
                }
            }

            // Set reply-to
            if (!string.IsNullOrEmpty(emailModel.ReplyTo) && IsValidEmail(emailModel.ReplyTo))
            {
                mailMessage.ReplyToList.Add(emailModel.ReplyTo);
            }

            // Add custom headers
            if (emailModel.CustomHeaders?.Any() == true)
            {
                foreach (var header in emailModel.CustomHeaders)
                {
                    mailMessage.Headers.Add(header.Key, header.Value);
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
                        var mailAttachment = new Attachment(memoryStream, attachment.FileName)
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
            var logoPath = Path.Combine(_env.WebRootPath, "site/images/mail_logo.png");
            var headerPath = Path.Combine(_env.WebRootPath, "site/images/mail_header.png");

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

        private void ValidateEmailModel(EmailModel emailModel)
        {
            if (emailModel == null)
                throw new ArgumentNullException(nameof(emailModel), "Email model cannot be null.");

            if (string.IsNullOrEmpty(emailModel.RecipientEmail))
                throw new ArgumentException("Recipient email cannot be null or empty.", nameof(emailModel.RecipientEmail));

            if (!IsValidEmail(emailModel.RecipientEmail))
                throw new ArgumentException("Invalid recipient email format.", nameof(emailModel.RecipientEmail));

            if (string.IsNullOrEmpty(emailModel.Subject))
                throw new ArgumentException("Subject cannot be null or empty.", nameof(emailModel.Subject));

            if (string.IsNullOrEmpty(emailModel.Body))
                throw new ArgumentException("Body cannot be null or empty.", nameof(emailModel.Body));
        }

        private bool IsValidEmail(string email)
        {
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

        public string ProcessTemplate(string template, Dictionary<string, string> placeholders)
        {
            var result = template;
            foreach (var placeholder in placeholders)
            {
                result = result.Replace($"{{{placeholder.Key}}}", placeholder.Value);
            }
            return result;
        }
    }
}