namespace DT.EmailWorker.Core.Configuration
{
    /// <summary>
    /// Email processing configuration settings
    /// </summary>
    public class ProcessingSettings
    {
        /// <summary>
        /// Maximum attachment size in MB
        /// </summary>
        public int MaxAttachmentSizeMB { get; set; } = 25;

        /// <summary>
        /// Maximum total email size in MB
        /// </summary>
        public int MaxEmailSizeMB { get; set; } = 50;

        /// <summary>
        /// Maximum number of recipients per email
        /// </summary>
        public int MaxRecipientsPerEmail { get; set; } = 100;

        /// <summary>
        /// Whether template processing is enabled
        /// </summary>
        public bool EnableTemplateProcessing { get; set; } = true;

        /// <summary>
        /// Whether email validation is enabled
        /// </summary>
        public bool EnableEmailValidation { get; set; } = true;

        /// <summary>
        /// Whether HTML optimization is enabled
        /// </summary>
        public bool EnableHtmlOptimization { get; set; } = true;

        /// <summary>
        /// Whether mobile optimization is enabled
        /// </summary>
        public bool EnableMobileOptimization { get; set; } = true;

        /// <summary>
        /// Whether CID image processing is enabled
        /// </summary>
        public bool EnableCidImageProcessing { get; set; } = true;

        /// <summary>
        /// Whether attachment processing is enabled
        /// </summary>
        public bool EnableAttachmentProcessing { get; set; } = true;

        /// <summary>
        /// Maximum number of placeholders allowed in templates
        /// </summary>
        public int MaxPlaceholders { get; set; } = 100;

        /// <summary>
        /// Placeholder pattern for template processing (regex)
        /// </summary>
        public string PlaceholderPattern { get; set; } = @"\{([^}]+)\}";

        /// <summary>
        /// Whether to validate placeholders during template processing
        /// </summary>
        public bool ValidatePlaceholders { get; set; } = true;

        /// <summary>
        /// Whether to log missing placeholders
        /// </summary>
        public bool LogMissingPlaceholders { get; set; } = true;

        /// <summary>
        /// Maximum processing time per email in minutes
        /// </summary>
        public int MaxProcessingTimeMinutes { get; set; } = 10;

        /// <summary>
        /// Whether to enable parallel attachment processing
        /// </summary>
        public bool EnableParallelAttachmentProcessing { get; set; } = true;

        /// <summary>
        /// Maximum number of concurrent attachment processes
        /// </summary>
        public int MaxConcurrentAttachmentProcesses { get; set; } = 3;

        /// <summary>
        /// Supported image formats for CID processing
        /// </summary>
        public List<string> SupportedImageFormats { get; set; } = new List<string>
        {
            "jpg", "jpeg", "png", "gif", "bmp", "webp"
        };

        /// <summary>
        /// Supported attachment formats
        /// </summary>
        public List<string> SupportedAttachmentFormats { get; set; } = new List<string>
        {
            "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "csv",
            "jpg", "jpeg", "png", "gif", "bmp", "zip", "rar", "7z"
        };

        /// <summary>
        /// Blocked attachment formats for security
        /// </summary>
        public List<string> BlockedAttachmentFormats { get; set; } = new List<string>
        {
            "exe", "bat", "cmd", "com", "pif", "scr", "vbs", "js"
        };

        /// <summary>
        /// Whether to enable virus scanning for attachments
        /// </summary>
        public bool EnableVirusScanning { get; set; } = false;

        /// <summary>
        /// Whether to compress large attachments
        /// </summary>
        public bool EnableAttachmentCompression { get; set; } = false;

        /// <summary>
        /// Compression threshold in MB
        /// </summary>
        public int CompressionThresholdMB { get; set; } = 5;
    }
}