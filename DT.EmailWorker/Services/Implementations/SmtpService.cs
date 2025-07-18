using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Models.DTOs;
using DT.EmailWorker.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.RegularExpressions;

namespace DT.EmailWorker.Services.Implementations
{
    /// <summary>
    /// SMTP service implementation using MailKit
    /// </summary>
    public class SmtpService : ISmtpService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<SmtpService> _logger;
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SmtpService(
            IOptions<SmtpSettings> smtpSettings,
            ILogger<SmtpService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }




        public async Task<bool> SendEmailAsync(EmailProcessingRequest request)
        {
            try
            {
                using var client = new SmtpClient();
                await ConnectToSmtpAsync(client);

                var message = CreateMimeMessage(request);
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
                        var message = CreateMimeMessage(request);
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

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }

        private async Task ConnectToSmtpAsync(SmtpClient client)
        {
            // Connect to SMTP server
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port,
                _smtpSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

            // Authenticate if credentials are provided
            if (!string.IsNullOrWhiteSpace(_smtpSettings.Username) &&
                !string.IsNullOrWhiteSpace(_smtpSettings.Password))
            {
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            }
        }

        private MimeMessage CreateMimeMessage(EmailProcessingRequest request)
        {
            var message = new MimeMessage();

            // From
            if (!string.IsNullOrWhiteSpace(_smtpSettings.FromName))
            {
                message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            }
            else
            {
                message.From.Add(MailboxAddress.Parse(_smtpSettings.FromEmail));
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

            // Body
            var bodyBuilder = new BodyBuilder();

            if (request.IsHtml)
            {
                bodyBuilder.HtmlBody = request.Body;
            }
            else
            {
                bodyBuilder.TextBody = request.Body;
            }

            // Attachments
            if (request.Attachments?.Any() == true)
            {
                foreach (var attachment in request.Attachments)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(attachment.Content))
                        {
                            // Base64 content
                            var bytes = Convert.FromBase64String(attachment.Content);
                            bodyBuilder.Attachments.Add(attachment.FileName, bytes,
                                ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
                        }
                        else if (!string.IsNullOrWhiteSpace(attachment.FilePath) && File.Exists(attachment.FilePath))
                        {
                            // File path
                            bodyBuilder.Attachments.Add(attachment.FilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add attachment {FileName}", attachment.FileName);
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }
    }
}