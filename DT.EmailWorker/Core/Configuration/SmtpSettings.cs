namespace DT.EmailWorker.Core.Configuration
{
    /// <summary>
    /// SMTP server configuration settings
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// SMTP server hostname or IP address
        /// </summary>
        public string Server { get; set; } = string.Empty;

        /// <summary>
        /// SMTP server port (typically 25, 587, or 465)
        /// </summary>
        public int Port { get; set; } = 25;

        /// <summary>
        /// SMTP username for authentication (optional)
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// SMTP password for authentication (optional)
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Sender email address
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Sender display name
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use SSL encryption
        /// </summary>
        public bool UseSSL { get; set; } = false;

        /// <summary>
        /// Whether to use TLS encryption (STARTTLS)
        /// </summary>
        public bool UseTLS { get; set; } = false;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to enable SMTP authentication
        /// </summary>
        public bool EnableAuthentication => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);

        /// <summary>
        /// Maximum number of concurrent SMTP connections
        /// </summary>
        public int MaxConcurrentConnections { get; set; } = 10;

        /// <summary>
        /// Whether to enable connection pooling
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// Connection pool timeout in minutes
        /// </summary>
        public int ConnectionPoolTimeoutMinutes { get; set; } = 5;

        /// <summary>
        /// Whether to validate SSL certificates
        /// </summary>
        public bool ValidateSSLCertificate { get; set; } = true;

        /// <summary>
        /// Retry attempts for SMTP connection failures
        /// </summary>
        public int RetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in seconds
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;

        /// <summary>
        /// Whether to enable SMTP logging
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Default reply-to email address
        /// </summary>
        public string? DefaultReplyTo { get; set; }

        /// <summary>
        /// Whether to request delivery receipts by default
        /// </summary>
        public bool RequestDeliveryReceipt { get; set; } = false;

        /// <summary>
        /// Whether to request read receipts by default
        /// </summary>
        public bool RequestReadReceipt { get; set; } = false;

        /// <summary>
        /// Custom SMTP headers to add to all emails
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
    }
}