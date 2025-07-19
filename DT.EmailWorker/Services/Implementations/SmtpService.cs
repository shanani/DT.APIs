using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Core.Engines; // ADD THIS
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// SMTP service implementation using MailKit with CID image processing
    /// </summary>
    public class SmtpService : ISmtpService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<SmtpService> _logger;
        private readonly CidImageProcessor _cidImageProcessor; // ADD THIS

        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SmtpService(
            IOptions<SmtpSettings> smtpSettings,
            ILogger<SmtpService> logger,
            CidImageProcessor cidImageProcessor) // ADD THIS
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
            _cidImageProcessor = cidImageProcessor; // ADD THIS
        }

        public async Task<bool> SendEmailAsync(EmailProcessingRequest request)
        {
            try
            {
                using var client = new SmtpClient();
                await ConnectToSmtpAsync(client);

                var message = await CreateMimeMessageAsync(request); // CHANGED TO ASYNC
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmails}", request.ToEmails);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmails}", request.ToEmails);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var client = new SmtpClient();
                await ConnectToSmtpAsync(client);
                await client.DisconnectAsync(true);
                _logger.LogInformation("SMTP connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP connection test failed");
                return false;
            }
        }

        private async Task ConnectToSmtpAsync(SmtpClient client)
        {
            await client.ConnectAsync(
                _smtpSettings.Server,
                _smtpSettings.Port,
                _smtpSettings.UseSSL ? SecureSocketOptions.SslOnConnect :
                _smtpSettings.UseTLS ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            // Authenticate if credentials are provided
            if (!string.IsNullOrWhiteSpace(_smtpSettings.Username) &&
                !string.IsNullOrWhiteSpace(_smtpSettings.Password))
            {
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            }
        }

        /// <summary>
        /// Create MIME message with automatic CID image processing
        /// </summary>
        private async Task<MimeMessage> CreateMimeMessageAsync(EmailProcessingRequest request)
        {
            var message = new MimeMessage();

            // From - FIXED: Use correct property names from SmtpSettings
            if (!string.IsNullOrWhiteSpace(_smtpSettings.SenderName))
            {
                message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            }
            else
            {
                message.From.Add(MailboxAddress.Parse(_smtpSettings.SenderEmail));
            }

            // To
            foreach (var email in request.ToEmails.Split(',', ';').Select(e => e.Trim()))
            {
                if (IsValidEmail(email))
                {
                    message.To.Add(MailboxAddress.Parse(email));
                }
            }

            // CC
            if (!string.IsNullOrWhiteSpace(request.CcEmails))
            {
                foreach (var email in request.CcEmails.Split(',', ';').Select(e => e.Trim()))
                {
                    if (IsValidEmail(email))
                    {
                        message.Cc.Add(MailboxAddress.Parse(email));
                    }
                }
            }

            // BCC
            if (!string.IsNullOrWhiteSpace(request.BccEmails))
            {
                foreach (var email in request.BccEmails.Split(',', ';').Select(e => e.Trim()))
                {
                    if (IsValidEmail(email))
                    {
                        message.Bcc.Add(MailboxAddress.Parse(email));
                    }
                }
            }

            // Subject
            message.Subject = request.Subject;

            // Body with CID processing
            var bodyBuilder = new BodyBuilder();

            if (request.IsHtml && !string.IsNullOrWhiteSpace(request.Body))
            {
                // 🚀 CRITICAL: Process base64 images to CID automatically
                var cidResult = await _cidImageProcessor.ProcessImagesAsync(request.Body);

                if (cidResult.IsSuccess)
                {
                    // Use processed HTML with CID references
                    bodyBuilder.HtmlBody = cidResult.ProcessedHtml;

                    // Add processed images as inline attachments
                    foreach (var cidImage in cidResult.ProcessedImages)
                    {
                        try
                        {
                            var imageBytes = Convert.FromBase64String(cidImage.Content);
                            using var memoryStream = new MemoryStream(imageBytes);

                            var linkedResource = bodyBuilder.LinkedResources.Add(
                                cidImage.FileName,
                                memoryStream);

                            linkedResource.ContentId = cidImage.ContentId;
                            linkedResource.ContentType.MediaType = cidImage.ContentType;

                            _logger.LogDebug("Added CID image: {ContentId} ({FileName})",
                                cidImage.ContentId, cidImage.FileName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to add CID image {ContentId}", cidImage.ContentId);
                        }
                    }

                    _logger.LogInformation("Processed {Count} base64 images to CID attachments",
                        cidResult.ProcessedImages.Count);
                }
                else
                {
                    // CID processing failed, use original content
                    bodyBuilder.HtmlBody = request.Body;
                    _logger.LogWarning("CID processing failed: {Error}. Using original HTML content.",
                        cidResult.ErrorMessage);
                }
            }
            else
            {
                bodyBuilder.TextBody = request.Body;
            }

            // Regular Attachments - Parse JSON string to attachment objects
            if (!string.IsNullOrWhiteSpace(request.Attachments))
            {
                try
                {
                    var attachments = JsonSerializer.Deserialize<List<AttachmentData>>(request.Attachments);
                    if (attachments?.Any() == true)
                    {
                        foreach (var attachment in attachments)
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(attachment.Content))
                                {
                                    // Base64 content
                                    var bytes = Convert.FromBase64String(attachment.Content);

                                    if (attachment.IsInline && !string.IsNullOrWhiteSpace(attachment.ContentId))
                                    {
                                        // Inline attachment with custom CID
                                        var linkedResource = bodyBuilder.LinkedResources.Add(
                                            attachment.FileName, bytes,
                                            ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
                                        linkedResource.ContentId = attachment.ContentId;

                                        _logger.LogDebug("Added custom inline attachment: {ContentId}", attachment.ContentId);
                                    }
                                    else
                                    {
                                        // Regular attachment
                                        bodyBuilder.Attachments.Add(attachment.FileName, bytes,
                                            ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));

                                        _logger.LogDebug("Added regular attachment: {FileName}", attachment.FileName);
                                    }
                                }
                                else if (!string.IsNullOrWhiteSpace(attachment.FilePath) && File.Exists(attachment.FilePath))
                                {
                                    // File path
                                    if (attachment.IsInline && !string.IsNullOrWhiteSpace(attachment.ContentId))
                                    {
                                        var linkedResource = bodyBuilder.LinkedResources.Add(attachment.FilePath);
                                        linkedResource.ContentId = attachment.ContentId;
                                    }
                                    else
                                    {
                                        bodyBuilder.Attachments.Add(attachment.FilePath);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to add attachment {FileName}", attachment.FileName);
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse attachments JSON");
                }
            }

            message.Body = bodyBuilder.ToMessageBody();
            return message;
        }

        public bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
        }

        public async Task<int> SendBulkEmailsAsync(IEnumerable<EmailProcessingRequest> requests)
        {
            var successCount = 0;
            var requestList = requests.ToList();

            _logger.LogInformation("Starting bulk email send for {Count} emails", requestList.Count);

            try
            {
                using var client = new SmtpClient();
                await ConnectToSmtpAsync(client);

                foreach (var request in requestList)
                {
                    try
                    {
                        var message = await CreateMimeMessageAsync(request);
                        await client.SendAsync(message);
                        successCount++;

                        _logger.LogDebug("Bulk email {Index}/{Total} sent to {ToEmails}",
                            successCount, requestList.Count, request.ToEmails);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send bulk email to {ToEmails}", request.ToEmails);
                    }
                }

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk email operation failed after {SuccessCount} successful sends", successCount);
            }

            _logger.LogInformation("Bulk email completed: {SuccessCount}/{TotalCount} sent successfully",
                successCount, requestList.Count);

            return successCount;
        }
    }
}