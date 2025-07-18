using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DT.EmailWorker.Core.Utilities
{
    /// <summary>
    /// Utility class for email validation
    /// </summary>
    public static class EmailValidator
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DomainRegex = new Regex(
            @"^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Validate single email address
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if email is valid</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim();

            // Basic format check
            if (!EmailRegex.IsMatch(email))
                return false;

            // Additional validation using MailAddress
            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate multiple email addresses
        /// </summary>
        /// <param name="emails">Comma or semicolon separated email addresses</param>
        /// <returns>List of validation results</returns>
        public static List<EmailValidationResult> ValidateEmails(string emails)
        {
            var results = new List<EmailValidationResult>();

            if (string.IsNullOrWhiteSpace(emails))
                return results;

            var emailList = emails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var email in emailList)
            {
                var trimmedEmail = email.Trim();
                results.Add(new EmailValidationResult
                {
                    Email = trimmedEmail,
                    IsValid = IsValidEmail(trimmedEmail),
                    ErrorMessage = IsValidEmail(trimmedEmail) ? null : "Invalid email format"
                });
            }

            return results;
        }

        /// <summary>
        /// Get only valid emails from a list
        /// </summary>
        /// <param name="emails">Comma or semicolon separated email addresses</param>
        /// <returns>List of valid email addresses</returns>
        public static List<string> GetValidEmails(string emails)
        {
            return ValidateEmails(emails)
                .Where(r => r.IsValid)
                .Select(r => r.Email)
                .ToList();
        }

        /// <summary>
        /// Validate email domain
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if domain is valid</returns>
        public static bool IsValidDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return false;

            var domain = email.Split('@').LastOrDefault();
            return !string.IsNullOrWhiteSpace(domain) && DomainRegex.IsMatch(domain);
        }

        /// <summary>
        /// Extract domain from email address
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Domain part of email or null if invalid</returns>
        public static string? ExtractDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return null;

            var domain = email.Split('@').LastOrDefault();
            return IsValidDomain(email) ? domain : null;
        }

        /// <summary>
        /// Normalize email address (lowercase, trim)
        /// </summary>
        /// <param name="email">Email address to normalize</param>
        /// <returns>Normalized email address</returns>
        public static string? NormalizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            email = email.Trim().ToLowerInvariant();
            return IsValidEmail(email) ? email : null;
        }

        /// <summary>
        /// Check if email domain is from common providers
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if from common provider</returns>
        public static bool IsCommonProvider(string email)
        {
            var domain = ExtractDomain(email);
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            var commonProviders = new[]
            {
                "gmail.com", "yahoo.com", "outlook.com", "hotmail.com",
                "live.com", "msn.com", "aol.com", "icloud.com",
                "protonmail.com", "mail.com"
            };

            return commonProviders.Contains(domain.ToLowerInvariant());
        }

        /// <summary>
        /// Validate email list and return summary
        /// </summary>
        /// <param name="emails">Email addresses to validate</param>
        /// <returns>Validation summary</returns>
        public static EmailValidationSummary ValidateEmailList(string emails)
        {
            var results = ValidateEmails(emails);

            return new EmailValidationSummary
            {
                TotalEmails = results.Count,
                ValidEmails = results.Count(r => r.IsValid),
                InvalidEmails = results.Count(r => !r.IsValid),
                ValidationResults = results
            };
        }
    }

    /// <summary>
    /// Email validation result
    /// </summary>
    public class EmailValidationResult
    {
        public string Email { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Email validation summary
    /// </summary>
    public class EmailValidationSummary
    {
        public int TotalEmails { get; set; }
        public int ValidEmails { get; set; }
        public int InvalidEmails { get; set; }
        public List<EmailValidationResult> ValidationResults { get; set; } = new();
        public bool HasValidEmails => ValidEmails > 0;
        public bool HasInvalidEmails => InvalidEmails > 0;
        public double SuccessRate => TotalEmails > 0 ? (double)ValidEmails / TotalEmails * 100 : 0;
    }
}