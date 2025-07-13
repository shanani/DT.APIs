using DT.APIs.Models;
using HtmlAgilityPack;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using MimeKit.Utils;
using System.Net.Mail; // Keep for MailPriority enum compatibility

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

        #region Public Methods

        /// <summary>
        /// Sends a single email with full normalization and validation
        /// </summary>
        /// <param name="emailModel">Email details</param>
        /// <returns>Task</returns>
        public async Task SendEmailAsync(EmailModel emailModel)
        {
            // Normalize and validate input (maintains full flexibility)
            NormalizeEmailModel(emailModel);
            ValidateEmailModel(emailModel);

            var mailSettings = _configuration.GetSection("MailSettings");

            // Use MailKit SMTP client
            using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
            var mailMessage = CreateMimeMessage(emailModel, mailSettings);

            try
            {
                // Port 25 specific connection (no encryption, no auth for internal SMTP)
                await smtpClient.ConnectAsync(
                    mailSettings["Server"],
                    int.Parse(mailSettings["Port"]),
                    SecureSocketOptions.None);

                // Only authenticate if credentials are provided
                var username = mailSettings["Username"];
                var password = mailSettings["Password"];
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await smtpClient.AuthenticateAsync(username, password);
                }

                await smtpClient.SendAsync(mailMessage);
                await smtpClient.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {RecipientEmail}", emailModel.RecipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {RecipientEmail}", emailModel.RecipientEmail);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sends bulk emails with concurrency control and detailed results
        /// </summary>
        /// <param name="bulkEmailModel">Bulk email details</param>
        /// <returns>Detailed results with success/failure counts</returns>
        public async Task<BulkEmailResultModel> SendBulkEmailAsync(BulkEmailModel bulkEmailModel)
        {
            // Normalize bulk email model
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

        /// <summary>
        /// Processes template with placeholders (maintains exact same signature)
        /// </summary>
        /// <param name="template">Template string with {placeholders}</param>
        /// <param name="placeholders">Dictionary of placeholder values</param>
        /// <returns>Processed template</returns>
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

        #endregion

        #region Private Email Creation Methods

        /// <summary>
        /// Creates MimeMessage from EmailModel using MailKit
        /// </summary>
        private MimeMessage CreateMimeMessage(EmailModel emailModel, IConfigurationSection mailSettings)
        {
            var message = new MimeMessage();

            // Set sender
            message.From.Add(new MailboxAddress(
                mailSettings["SenderName"],
                mailSettings["SenderEmail"]));

            // Set primary recipient
            message.To.Add(new MailboxAddress(
                emailModel.RecipientName ?? "",
                emailModel.RecipientEmail));

            // Add CC recipients (only valid emails)
            if (emailModel.CC?.Any() == true)
            {
                foreach (var cc in emailModel.CC.Where(IsValidEmail))
                {
                    try
                    {
                        message.Cc.Add(MailboxAddress.Parse(cc));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add CC recipient: {CC}", cc);
                    }
                }
            }

            // Add BCC recipients (only valid emails)
            if (emailModel.BCC?.Any() == true)
            {
                foreach (var bcc in emailModel.BCC.Where(IsValidEmail))
                {
                    try
                    {
                        message.Bcc.Add(MailboxAddress.Parse(bcc));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add BCC recipient: {BCC}", bcc);
                    }
                }
            }

            // Set reply-to
            if (!string.IsNullOrWhiteSpace(emailModel.ReplyTo) && IsValidEmail(emailModel.ReplyTo))
            {
                message.ReplyTo.Add(MailboxAddress.Parse(emailModel.ReplyTo));
            }

            // Set subject
            message.Subject = emailModel.Subject;

            // Convert priority (maintain compatibility with System.Net.Mail)
            message.Priority = ConvertPriority(emailModel.Priority);

            // Add professional email headers for better deliverability
            AddProfessionalHeaders(message, mailSettings);

            // Add custom headers
            if (emailModel.CustomHeaders?.Any() == true)
            {
                foreach (var header in emailModel.CustomHeaders)
                {
                    try
                    {
                        message.Headers.Add(header.Key, header.Value);
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
                message.Headers.Add("Return-Receipt-To", mailSettings["SenderEmail"]);
            }

            if (emailModel.RequestReadReceipt)
            {
                message.Headers.Add("Disposition-Notification-To", mailSettings["SenderEmail"]);
            }

            // Create message body with mobile optimization
            message.Body = CreateMessageBody(emailModel);

            return message;
        }

        /// <summary>
        /// Creates message body with proper MIME structure and mobile optimization
        /// </summary>
        private MimeEntity CreateMessageBody(EmailModel emailModel)
        {
            if (emailModel.Attachments?.Any() != true)
            {
                // Simple message without attachments
                return emailModel.IsBodyHtml
                    ? new TextPart(TextFormat.Html) { Text = OptimizeHtmlForEmailClients(emailModel.Body) }
                    : new TextPart(TextFormat.Plain) { Text = emailModel.Body };
            }

            // Complex message with attachments
            var multipart = new Multipart("mixed");

            // Add text/HTML part
            if (emailModel.IsBodyHtml)
            {
                var htmlPart = CreateHtmlPartWithInlineImages(emailModel);
                multipart.Add(htmlPart);
            }
            else
            {
                multipart.Add(new TextPart(TextFormat.Plain) { Text = emailModel.Body });
            }

            // Add regular attachments (non-inline)
            foreach (var attachment in emailModel.Attachments.Where(a => !a.IsInline))
            {
                try
                {
                    var fileBytes = Convert.FromBase64String(attachment.Content);
                    var mimePart = new MimePart(attachment.ContentType ?? "application/octet-stream")
                    {
                        Content = new MimeContent(new MemoryStream(fileBytes)),
                        ContentDisposition = new ContentDisposition("attachment"),
                        FileName = attachment.FileName
                    };
                    multipart.Add(mimePart);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process attachment {FileName}", attachment.FileName);
                }
            }

            return multipart;
        }

        /// <summary>
        /// Creates HTML part with inline images using proper multipart/related structure
        /// </summary>
        private MimeEntity CreateHtmlPartWithInlineImages(EmailModel emailModel)
        {
            var inlineAttachments = emailModel.Attachments?.Where(a => a.IsInline).ToList();

            if (inlineAttachments?.Any() != true)
            {
                return new TextPart(TextFormat.Html) { Text = OptimizeHtmlForEmailClients(emailModel.Body) };
            }

            var related = new Multipart("related");

            // Add HTML content
            related.Add(new TextPart(TextFormat.Html) { Text = OptimizeHtmlForEmailClients(emailModel.Body) });

            // Add inline images with proper cid: references
            foreach (var inlineAttachment in inlineAttachments)
            {
                try
                {
                    var fileBytes = Convert.FromBase64String(inlineAttachment.Content);
                    var image = new MimePart(inlineAttachment.ContentType ?? "image/png")
                    {
                        Content = new MimeContent(new MemoryStream(fileBytes)),
                        ContentDisposition = new ContentDisposition("inline"),
                        ContentId = inlineAttachment.ContentId ?? Guid.NewGuid().ToString()
                    };
                    related.Add(image);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process inline attachment {ContentId}", inlineAttachment.ContentId);
                }
            }

            return related;
        }

        #endregion

        #region Professional Email Headers and Mobile Optimization

        /// <summary>
        /// Adds professional email headers for better deliverability and client compatibility
        /// </summary>
        private void AddProfessionalHeaders(MimeMessage message, IConfigurationSection mailSettings)
        {
            // Professional email identification
            message.Headers.Add("X-Mailer", "DT APIs Professional Email System v2.0");
            message.Headers.Add("X-Message-Source", "DT-API-System");
            message.Headers.Add("X-Auto-Response-Suppress", "DR, OOF, AutoReply");

            // Generate proper Message-ID for email threading
            var domain = mailSettings["SenderEmail"]?.Split('@')[1] ?? "stc.com.sa";
            message.MessageId = MimeUtils.GenerateMessageId(domain);

            // Prevent auto-forwarding loops
            message.Headers.Add("X-Auto-Forward-Count", "0");
        }

        /// <summary>
        /// Optimizes HTML content for mobile email clients and cross-client compatibility
        /// </summary>
        private string OptimizeHtmlForEmailClients(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return htmlContent;

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // MOBILE: Add critical meta tags for mobile email clients
                EnsureMobileMetaTags(doc);

                // MOBILE: Optimize body for mobile rendering
                OptimizeBodyForMobile(doc);

                // MOBILE: Fix images for mobile email clients
                OptimizeImagesForMobile(doc);

                // MOBILE: Ensure tables work on mobile (common email layout)
                OptimizeTablesForMobile(doc);

                return doc.DocumentNode.OuterHtml;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to optimize HTML for mobile, using original");
                return htmlContent; // Return original if processing fails
            }
        }

        /// <summary>
        /// Ensures essential meta tags for mobile email clients
        /// </summary>
        private void EnsureMobileMetaTags(HtmlDocument doc)
        {
            var head = doc.DocumentNode.SelectSingleNode("//head");
            if (head == null)
            {
                // Create head if it doesn't exist
                var html = doc.DocumentNode.SelectSingleNode("//html") ?? doc.DocumentNode;
                head = doc.CreateElement("head");
                html.PrependChild(head);
            }

            // Critical mobile viewport (prevents tiny text on phones)
            if (doc.DocumentNode.SelectSingleNode("//meta[@name='viewport']") == null)
            {
                var viewport = doc.CreateElement("meta");
                viewport.SetAttributeValue("name", "viewport");
                viewport.SetAttributeValue("content", "width=device-width, initial-scale=1.0");
                head.PrependChild(viewport);
            }

            // Mobile-friendly charset
            if (doc.DocumentNode.SelectSingleNode("//meta[@charset]") == null)
            {
                var charset = doc.CreateElement("meta");
                charset.SetAttributeValue("charset", "UTF-8");
                head.PrependChild(charset);
            }

            // Outlook mobile compatibility
            var compatibility = doc.CreateElement("meta");
            compatibility.SetAttributeValue("http-equiv", "X-UA-Compatible");
            compatibility.SetAttributeValue("content", "IE=edge");
            head.PrependChild(compatibility);
        }

        /// <summary>
        /// Optimizes body element for mobile email clients
        /// </summary>
        private void OptimizeBodyForMobile(HtmlDocument doc)
        {
            var body = doc.DocumentNode.SelectSingleNode("//body");
            if (body != null)
            {
                var currentStyle = body.GetAttributeValue("style", "");

                // Mobile-friendly font and spacing
                var mobileStyles = new Dictionary<string, string>
                {
                    ["font-family"] = "Arial, 'Helvetica Neue', Helvetica, sans-serif",
                    ["line-height"] = "1.6",
                    ["color"] = "#333",
                    ["margin"] = "0",
                    ["padding"] = "10px", // Mobile padding
                    ["font-size"] = "16px", // Prevents zoom on mobile
                    ["-webkit-text-size-adjust"] = "100%", // iOS text size
                    ["-ms-text-size-adjust"] = "100%" // Windows Phone text size
                };

                foreach (var style in mobileStyles)
                {
                    if (!currentStyle.Contains(style.Key))
                    {
                        currentStyle += $" {style.Key}: {style.Value};";
                    }
                }

                body.SetAttributeValue("style", currentStyle);
            }
        }

        /// <summary>
        /// Optimizes images for mobile email clients
        /// </summary>
        private void OptimizeImagesForMobile(HtmlDocument doc)
        {
            var images = doc.DocumentNode.SelectNodes("//img");
            if (images != null)
            {
                foreach (var img in images)
                {
                    var currentStyle = img.GetAttributeValue("style", "");

                    // Mobile-responsive image styles
                    var mobileImageStyles = new Dictionary<string, string>
                    {
                        ["max-width"] = "100%", // Never exceed container width
                        ["height"] = "auto", // Maintain aspect ratio
                        ["display"] = "block", // Better spacing in email clients
                        ["margin"] = "0 auto" // Center images
                    };

                    foreach (var style in mobileImageStyles)
                    {
                        if (!currentStyle.Contains(style.Key))
                        {
                            currentStyle += $" {style.Key}: {style.Value};";
                        }
                    }

                    img.SetAttributeValue("style", currentStyle);

                    // Ensure alt text for accessibility and mobile screen readers
                    if (string.IsNullOrEmpty(img.GetAttributeValue("alt", "")))
                    {
                        img.SetAttributeValue("alt", "Image");
                    }
                }
            }
        }

        /// <summary>
        /// Optimizes tables for mobile email clients (common email layout)
        /// </summary>
        private void OptimizeTablesForMobile(HtmlDocument doc)
        {
            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    var currentStyle = table.GetAttributeValue("style", "");

                    // Mobile-friendly table styles
                    if (!currentStyle.Contains("width"))
                    {
                        currentStyle += " width: 100%; max-width: 600px;"; // Responsive width
                    }
                    if (!currentStyle.Contains("border-collapse"))
                    {
                        currentStyle += " border-collapse: collapse;"; // Better mobile rendering
                    }

                    table.SetAttributeValue("style", currentStyle);

                    // Ensure table cells are mobile-friendly
                    var cells = table.SelectNodes(".//td | .//th");
                    if (cells != null)
                    {
                        foreach (var cell in cells)
                        {
                            var cellStyle = cell.GetAttributeValue("style", "");
                            if (!cellStyle.Contains("padding"))
                            {
                                cellStyle += " padding: 8px;"; // Touch-friendly padding
                            }
                            cell.SetAttributeValue("style", cellStyle);
                        }
                    }
                }
            }
        }

        #endregion

        #region Bulk Email Processing

        /// <summary>
        /// Sends individual emails with concurrency control
        /// </summary>
        private async Task SendIndividualEmails(BulkEmailModel bulkEmailModel, BulkEmailResultModel result)
        {
            // Use semaphore to limit concurrent connections (prevents overwhelming SMTP server)
            using var semaphore = new SemaphoreSlim(5, 5); // Max 5 concurrent emails
            var successCounter = 0;
            var failureCounter = 0;

            var tasks = bulkEmailModel.Recipients.Select(async recipient =>
            {
                await semaphore.WaitAsync();
                try
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
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            // Update the result counters
            result.SuccessfulSends = successCounter;
            result.FailedSends = failureCounter;
        }

        /// <summary>
        /// Sends single email to multiple recipients (all in TO/CC fields)
        /// </summary>
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

        #endregion

        #region Input Normalization and Validation (Maintains Full Flexibility)

        /// <summary>
        /// Normalizes email model - handles all null/empty inputs gracefully (UNCHANGED)
        /// </summary>
        private void NormalizeEmailModel(EmailModel emailModel)
        {
            if (emailModel == null) return;

            // Initialize collections if null (maintains flexibility)
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

        /// <summary>
        /// Normalizes bulk email model (UNCHANGED)
        /// </summary>
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

        /// <summary>
        /// Validates email model - throws exceptions for invalid required fields (UNCHANGED)
        /// </summary>
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

        #endregion

        #region Utility Methods

        /// <summary>
        /// Converts System.Net.Mail.MailPriority to MimeKit.MessagePriority
        /// </summary>
        private MessagePriority ConvertPriority(System.Net.Mail.MailPriority priority)
        {
            return priority switch
            {
                System.Net.Mail.MailPriority.Low => MessagePriority.NonUrgent,
                System.Net.Mail.MailPriority.High => MessagePriority.Urgent,
                _ => MessagePriority.Normal
            };
        }

        /// <summary>
        /// Validates email address using MailboxAddress (more robust than regex)
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = MailboxAddress.Parse(email);
                return !string.IsNullOrEmpty(addr.Address);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}