using DT.EmailWorker.Models.DTOs;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// SMTP service interface for sending emails
    /// </summary>
    public interface ISmtpService
    {
        /// <summary>
        /// Send a single email
        /// </summary>
        /// <param name="request">Email processing request</param>
        /// <returns>True if successful</returns>
        Task<bool> SendEmailAsync(EmailProcessingRequest request);

        /// <summary>
        /// Send multiple emails in bulk
        /// </summary>
        /// <param name="requests">Collection of email requests</param>
        /// <returns>Number of successfully sent emails</returns>
        Task<int> SendBulkEmailsAsync(IEnumerable<EmailProcessingRequest> requests);

        /// <summary>
        /// Test SMTP connection
        /// </summary>
        /// <returns>True if connection successful</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Validate email address format
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if valid</returns>
        bool IsValidEmail(string email);
    }
}