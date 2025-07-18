using DT.EmailWorker.Models.DTOs;

namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Service for SMTP email sending operations
    /// </summary>
    public interface ISmtpService
    {
        /// <summary>
        /// Send email using SMTP
        /// </summary>
        /// <param name="request">Email processing request</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendEmailAsync(EmailProcessingRequest request);

        /// <summary>
        /// Test SMTP connection
        /// </summary>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Send bulk emails efficiently
        /// </summary>
        /// <param name="requests">List of email requests</param>
        /// <returns>Number of successfully sent emails</returns>
        Task<int> SendBulkEmailsAsync(IEnumerable<EmailProcessingRequest> requests);

        /// <summary>
        /// Validate email address format
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if email format is valid</returns>
        bool IsValidEmail(string email);
    }
}