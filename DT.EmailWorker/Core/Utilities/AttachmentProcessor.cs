using DT.EmailWorker.Models.Entities;
using Microsoft.Extensions.Logging;

namespace DT.EmailWorker.Core.Utilities
{
    /// <summary>
    /// Utility class for processing email attachments
    /// </summary>
    public class AttachmentProcessor
    {
        private readonly ILogger<AttachmentProcessor> _logger;
        private const int MaxAttachmentSizeMB = 25; // Default max size
        private const long MaxAttachmentSizeBytes = MaxAttachmentSizeMB * 1024 * 1024;

        private static readonly Dictionary<string, string> MimeTypeMappings = new()
        {
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".txt", "text/plain" },
            { ".rtf", "application/rtf" },
            { ".zip", "application/zip" },
            { ".rar", "application/x-rar-compressed" },
            { ".7z", "application/x-7z-compressed" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".svg", "image/svg+xml" },
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".mp4", "video/mp4" },
            { ".avi", "video/x-msvideo" },
            { ".mov", "video/quicktime" }
        };

        public AttachmentProcessor(ILogger<AttachmentProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process and validate attachments
        /// </summary>
        /// <param name="attachments">List of attachments to process</param>
        /// <param name="maxSizeMB">Maximum attachment size in MB</param>
        /// <returns>Processing result</returns>
        public async Task<AttachmentProcessingResult> ProcessAttachmentsAsync(
            List<EmailAttachment> attachments,
            int maxSizeMB = MaxAttachmentSizeMB)
        {
            var result = new AttachmentProcessingResult();
            var maxSizeBytes = maxSizeMB * 1024 * 1024;

            if (attachments == null || !attachments.Any())
            {
                result.IsSuccess = true;
                return result;
            }

            _logger.LogDebug("Processing {Count} attachments", attachments.Count);

            foreach (var attachment in attachments)
            {
                try
                {
                    var validation = await ValidateAttachmentAsync(attachment, maxSizeBytes);
                    result.ValidationResults.Add(validation);

                    if (!validation.IsValid)
                    {
                        result.HasErrors = true;
                        continue;
                    }

                    // Process the attachment
                    var processed = await ProcessSingleAttachmentAsync(attachment);
                    if (processed != null)
                    {
                        result.ProcessedAttachments.Add(processed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process attachment {FileName}", attachment.FileName);
                    result.HasErrors = true;
                    result.ValidationResults.Add(new AttachmentValidationResult
                    {
                        FileName = attachment.FileName,
                        IsValid = false,
                        ErrorMessage = $"Processing error: {ex.Message}"
                    });
                }
            }

            result.IsSuccess = !result.HasErrors;
            result.TotalSize = result.ProcessedAttachments.Sum(a => a.FileSize);

            _logger.LogDebug("Attachment processing completed. Success: {IsSuccess}, Count: {Count}, Total Size: {TotalSize} bytes",
                result.IsSuccess, result.ProcessedAttachments.Count, result.TotalSize);

            return result;
        }

        /// <summary>
        /// Validate single attachment
        /// </summary>
        /// <param name="attachment">Attachment to validate</param>
        /// <param name="maxSizeBytes">Maximum size in bytes</param>
        /// <returns>Validation result</returns>
        public async Task<AttachmentValidationResult> ValidateAttachmentAsync(EmailAttachment attachment, long maxSizeBytes = MaxAttachmentSizeBytes)
        {
            var result = new AttachmentValidationResult
            {
                FileName = attachment.FileName,
                IsValid = true
            };

            try
            {
                // Validate file name
                if (string.IsNullOrWhiteSpace(attachment.FileName))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "File name is required";
                    return result;
                }

                // Check for invalid characters
                if (HasInvalidFileNameCharacters(attachment.FileName))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "File name contains invalid characters";
                    return result;
                }

                // Validate file size
                if (!string.IsNullOrWhiteSpace(attachment.Content))
                {
                    // Base64 content
                    try
                    {
                        var bytes = Convert.FromBase64String(attachment.Content);
                        result.FileSizeBytes = bytes.Length;

                        if (bytes.Length > maxSizeBytes)
                        {
                            result.IsValid = false;
                            result.ErrorMessage = $"File size ({FormatFileSize(bytes.Length)}) exceeds maximum allowed size ({FormatFileSize(maxSizeBytes)})";
                            return result;
                        }
                    }
                    catch (FormatException)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "Invalid Base64 content";
                        return result;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(attachment.FilePath))
                {
                    // File path
                    if (!File.Exists(attachment.FilePath))
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "File does not exist";
                        return result;
                    }

                    var fileInfo = new FileInfo(attachment.FilePath);
                    result.FileSizeBytes = fileInfo.Length;

                    if (fileInfo.Length > maxSizeBytes)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"File size ({FormatFileSize(fileInfo.Length)}) exceeds maximum allowed size ({FormatFileSize(maxSizeBytes)})";
                        return result;
                    }
                }
                else
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Either Content or FilePath must be provided";
                    return result;
                }

                // Validate content type
                var contentType = DetermineContentType(attachment.FileName, attachment.ContentType);
                if (string.IsNullOrWhiteSpace(contentType))
                {
                    result.ErrorMessage = "Unknown file type";
                    // Still valid, but with warning
                }

                result.DetectedContentType = contentType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate attachment {FileName}", attachment.FileName);
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Process single attachment
        /// </summary>
        /// <param name="attachment">Attachment to process</param>
        /// <returns>Processed attachment</returns>
        private async Task<EmailAttachment> ProcessSingleAttachmentAsync(EmailAttachment attachment)
        {
            // Ensure content type is set
            if (string.IsNullOrWhiteSpace(attachment.ContentType))
            {
                attachment.ContentType = DetermineContentType(attachment.FileName, null);
            }

            // If file path is provided, convert to base64
            if (!string.IsNullOrWhiteSpace(attachment.FilePath) && File.Exists(attachment.FilePath))
            {
                var bytes = await File.ReadAllBytesAsync(attachment.FilePath);
                attachment.Content = Convert.ToBase64String(bytes);
                attachment.FileSize = bytes.Length;

                // Clear file path for security
                attachment.FilePath = null;
            }
            else if (!string.IsNullOrWhiteSpace(attachment.Content))
            {
                var bytes = Convert.FromBase64String(attachment.Content);
                attachment.FileSize = bytes.Length;
            }

            return attachment;
        }

        /// <summary>
        /// Determine content type from file name and provided type
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="providedContentType">Provided content type</param>
        /// <returns>Determined content type</returns>
        public static string DetermineContentType(string fileName, string? providedContentType)
        {
            // Use provided content type if valid
            if (!string.IsNullOrWhiteSpace(providedContentType) && providedContentType.Contains("/"))
            {
                return providedContentType;
            }

            // Determine from file extension
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(extension) && MimeTypeMappings.TryGetValue(extension, out var mimeType))
            {
                return mimeType;
            }

            return "application/octet-stream";
        }

        /// <summary>
        /// Check if file name has invalid characters
        /// </summary>
        /// <param name="fileName">File name to check</param>
        /// <returns>True if has invalid characters</returns>
        private static bool HasInvalidFileNameCharacters(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return fileName.IndexOfAny(invalidChars) >= 0;
        }

        /// <summary>
        /// Format file size for display
        /// </summary>
        /// <param name="sizeInBytes">Size in bytes</param>
        /// <returns>Formatted size string</returns>
        public static string FormatFileSize(long sizeInBytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (sizeInBytes >= GB)
                return $"{sizeInBytes / (double)GB:F2} GB";
            if (sizeInBytes >= MB)
                return $"{sizeInBytes / (double)MB:F2} MB";
            if (sizeInBytes >= KB)
                return $"{sizeInBytes / (double)KB:F2} KB";

            return $"{sizeInBytes} bytes";
        }

        /// <summary>
        /// Get supported file extensions
        /// </summary>
        /// <returns>List of supported extensions</returns>
        public static List<string> GetSupportedExtensions()
        {
            return MimeTypeMappings.Keys.ToList();
        }

        /// <summary>
        /// Check if file extension is supported
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>True if supported</returns>
        public static bool IsSupportedFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return !string.IsNullOrWhiteSpace(extension) && MimeTypeMappings.ContainsKey(extension);
        }
    }

    /// <summary>
    /// Attachment processing result
    /// </summary>
    public class AttachmentProcessingResult
    {
        public bool IsSuccess { get; set; }
        public bool HasErrors { get; set; }
        public long TotalSize { get; set; }
        public List<EmailAttachment> ProcessedAttachments { get; set; } = new();
        public List<AttachmentValidationResult> ValidationResults { get; set; } = new();
    }

    /// <summary>
    /// Attachment validation result
    /// </summary>
    public class AttachmentValidationResult
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public long FileSizeBytes { get; set; }
        public string? DetectedContentType { get; set; }
    }
}