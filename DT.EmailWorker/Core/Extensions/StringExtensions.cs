using System.Text.RegularExpressions;

namespace DT.EmailWorker.Core.Extensions
{
    /// <summary>
    /// String extension methods for email processing
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Truncate string to specified length
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Replace template placeholders
        /// </summary>
        public static string ReplacePlaceholders(this string template, Dictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(template) || placeholders == null || !placeholders.Any())
                return template;

            var result = template;
            foreach (var placeholder in placeholders)
            {
                result = result.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
            }
            return result;
        }

        /// <summary>
        /// Extract placeholders from template
        /// </summary>
        public static List<string> ExtractPlaceholders(this string template)
        {
            if (string.IsNullOrEmpty(template))
                return new List<string>();

            var regex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
            return regex.Matches(template)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Sanitize HTML content
        /// </summary>
        public static string SanitizeHtml(this string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Basic HTML sanitization - remove script tags and dangerous attributes
            var sanitized = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            sanitized = Regex.Replace(sanitized, @"javascript:", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=", "", RegexOptions.IgnoreCase);

            return sanitized;
        }

        /// <summary>
        /// Check if string is valid email
        /// </summary>
        public static bool IsValidEmail(this string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
            return emailRegex.IsMatch(email);
        }

        /// <summary>
        /// Convert to safe file name
        /// </summary>
        public static string ToSafeFileName(this string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return sanitized.Length > 255 ? sanitized.Substring(0, 255) : sanitized;
        }
    }
}