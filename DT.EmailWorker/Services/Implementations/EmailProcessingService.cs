using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Services.Interfaces;
using HtmlAgilityPack;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Net.Mail;

namespace DT.EmailWorker.Services.Implementations
{
    public class EmailProcessingService : IEmailProcessingService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailProcessingService> _logger;

        public EmailProcessingService(IConfiguration configuration, ILogger<EmailProcessingService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        #region Main Interface Method

        public async Task ProcessEmailAsync(object emailData)
        {
            if (emailData is EmailProcessingRequest request)
            {
                await SendEmailFromRequestAsync(request);
            }
            else
            {
                throw new ArgumentException("Invalid email data type", nameof(emailData));
            }
        }

        #endregion

        #region Core Email Sending (From Proven EmailService)

        /// <summary>
        /// Send email from EmailProcessingRequest (queue processing)
        /// </summary>
        private async Task SendEmailFromRequestAsync(EmailProcessingRequest request)
        {
            var mailSettings = _configuration.GetSection("SmtpSettings");

            using var smtpClient = new System.Net.Mail.SmtpClient();
            var mailMessage = CreateMimeMessageFromRequest(request, mailSettings);

            try
            {
                await smtpClient.ConnectAsync(
                    mailSettings["Server"],
                    int.Parse(mailSettings["Port"]),
                    SecureSocketOptions.None);

                var username = mailSettings["Username"];
                var password = mailSettings["Password"];
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await smtpClient.AuthenticateAsync(username, password);
                }

                await smtpClient.SendAsync(mailMessage);
                await smtpClient.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmails}", request.ToEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmails}", request.ToEmails);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create MIME message from EmailProcessingRequest
        /// </summary>
        private MimeMessage CreateMimeMessageFromRequest(EmailProcessingRequest request, IConfigurationSection mailSettings)
        {
            var message = new MimeMessage();

            // Set sender
            message.From.Add(new MailboxAddress(
                mailSettings["SenderName"] ?? "System",
                mailSettings["SenderEmail"]
            ));

            // Set recipients
            var toEmails = request.ToEmails.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var email in toEmails)
            {
                if (IsValidEmail(email.Trim()))
                {
                    message.To.Add(new MailboxAddress(string.Empty, email.Trim()));
                }
            }

            // Add CC recipients if provided
            if (!string.IsNullOrEmpty(request.CcEmails))
            {
                var ccEmails = request.CcEmails.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var email in ccEmails)
                {
                    if (IsValidEmail(email.Trim()))
                    {
                        message.Cc.Add(new MailboxAddress(string.Empty, email.Trim()));
                    }
                }
            }

            // Add BCC recipients if provided
            if (!string.IsNullOrEmpty(request.BccEmails))
            {
                var bccEmails = request.BccEmails.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var email in bccEmails)
                {
                    if (IsValidEmail(email.Trim()))
                    {
                        message.Bcc.Add(new MailboxAddress(string.Empty, email.Trim()));
                    }
                }
            }

            // Set subject
            message.Subject = request.Subject ?? string.Empty;

            // 🎯 KEY: Use BodyBuilder for CID support (from proven EmailService)
            var bodyBuilder = new BodyBuilder();

            if (request.IsHtml)
            {
                bodyBuilder.HtmlBody = OptimizeHtmlForEmailClients(request.Body);
            }
            else
            {
                bodyBuilder.TextBody = request.Body ?? string.Empty;
            }

            // 🚀 CID SUPPORT: Handle attachments if present
            if (!string.IsNullOrEmpty(request.Attachments))
            {
                try
                {
                    var attachments = System.Text.Json.JsonSerializer.Deserialize<List<EmailAttachmentDto>>(request.Attachments);
                    if (attachments?.Any() == true)
                    {
                        foreach (var attachment in attachments)
                        {
                            ProcessAttachment(bodyBuilder, attachment);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process attachments JSON");
                }
            }

            message.Body = bodyBuilder.ToMessageBody();
            return message;
        }

        /// <summary>
        /// Process attachment with CID support (from proven EmailService)
        /// </summary>
        private void ProcessAttachment(BodyBuilder bodyBuilder, EmailAttachmentDto attachment)
        {
            try
            {
                var fileBytes = Convert.FromBase64String(attachment.Content);
                using var memoryStream = new MemoryStream(fileBytes);

                if (attachment.IsInline && !string.IsNullOrWhiteSpace(attachment.ContentId))
                {
                    // ✅ INLINE ATTACHMENT with CID support
                    var linkedResource = bodyBuilder.LinkedResources.Add(
                        attachment.FileName,
                        memoryStream
                    );
                    linkedResource.ContentId = attachment.ContentId;

                    if (!string.IsNullOrWhiteSpace(attachment.ContentType))
                    {
                        linkedResource.ContentType.MediaType = attachment.ContentType;
                    }

                    _logger.LogDebug("Added inline attachment with CID: {ContentId}", attachment.ContentId);
                }
                else
                {
                    // ✅ REGULAR ATTACHMENT
                    var mimeAttachment = bodyBuilder.Attachments.Add(
                        attachment.FileName,
                        memoryStream
                    );

                    if (!string.IsNullOrWhiteSpace(attachment.ContentType))
                    {
                        mimeAttachment.ContentType.MediaType = attachment.ContentType;
                    }

                    _logger.LogDebug("Added regular attachment: {FileName}", attachment.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process attachment {FileName}", attachment.FileName);
            }
        }

        #endregion

        #region Mobile Optimization (Essential from EmailService)

        /// <summary>
        /// Optimize HTML for mobile email clients (from proven EmailService)
        /// </summary>
        private string OptimizeHtmlForEmailClients(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return htmlContent;

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Add critical meta tags for mobile email clients
                EnsureMobileMetaTags(doc);

                // Optimize images for mobile
                OptimizeImagesForMobile(doc);

                return doc.DocumentNode.OuterHtml;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to optimize HTML for mobile, using original");
                return htmlContent;
            }
        }

        private void EnsureMobileMetaTags(HtmlDocument doc)
        {
            var head = doc.DocumentNode.SelectSingleNode("//head");
            if (head == null)
            {
                var html = doc.DocumentNode.SelectSingleNode("//html") ?? doc.DocumentNode;
                head = doc.CreateElement("head");
                html.PrependChild(head);
            }

            if (doc.DocumentNode.SelectSingleNode("//meta[@name='viewport']") == null)
            {
                var viewport = doc.CreateElement("meta");
                viewport.SetAttributeValue("name", "viewport");
                viewport.SetAttributeValue("content", "width=device-width, initial-scale=1.0");
                head.PrependChild(viewport);
            }
        }

        private void OptimizeImagesForMobile(HtmlDocument doc)
        {
            var images = doc.DocumentNode.SelectNodes("//img");
            if (images != null)
            {
                foreach (var img in images)
                {
                    var currentStyle = img.GetAttributeValue("style", "");
                    if (!currentStyle.Contains("max-width"))
                    {
                        currentStyle += " max-width: 100%; height: auto;";
                        img.SetAttributeValue("style", currentStyle);
                    }

                    if (string.IsNullOrEmpty(img.GetAttributeValue("alt", "")))
                    {
                        img.SetAttributeValue("alt", "Image");
                    }
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Validate email address using MailboxAddress
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

    #region Supporting DTOs

    public class EmailAttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string? ContentId { get; set; }
        public bool IsInline { get; set; } = false;
    }

    #endregion
}