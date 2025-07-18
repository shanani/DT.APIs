using DT.EmailWorker.Models.Entities;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DT.EmailWorker.Core.Utilities
{
    /// <summary>
    /// Attachment processing utility class
    /// </summary>
    public class AttachmentProcessor
    {
        private readonly ILogger<AttachmentProcessor> _logger;
        private const int MaxAttachmentSizeMB = 25;
        private const long MaxAttachmentSizeBytes = MaxAttachmentSizeMB * 1024 * 1024;

        // Allowed MIME types for security
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "text/plain",
            "text/csv",
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/webp",
            "application/zip",
            "application/x-zip-compressed",
            "application/json",
            "application/xml",
            "text/xml"
        };

        public AttachmentProcessor(ILogger<AttachmentProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process and validate attachments
        /// </summary>
        /// <param name="attachments">List of attachments to process</param>
        /// <returns>Processing result</returns>
        public AttachmentProcessingResult ProcessAttachments(List<EmailAttachment>? attachments)
        {
            var result = new AttachmentProcessingResult();

            if (attachments == null || !attachments.Any())
            {
                result.IsValid = true;
                return result;
            }

            long totalSize = 0;
            var validAttachments = new List<EmailAttachment>();
            var errors = new List<string>();

            foreach (var attachment in attachments)
            {
                try
                {
                    var validationResult = ValidateAttachment(attachment);
                    if (validationResult.IsValid)
                    {
                        totalSize += validationResult.SizeBytes;
                        validAttachments.Add(attachment);
                    }
                    else
                    {
                        errors.AddRange(validationResult.Errors);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process attachment {FileName}", attachment.FileName);
                    errors.Add($"Failed to process attachment {attachment.FileName}: {ex.Message}");
                }
            }

            // Check total size limit
            if (totalSize > MaxAttachmentSizeBytes)
            {
                errors.Add($"Total attachment size ({totalSize / 1024 / 1024:F1} MB) exceeds limit of {MaxAttachmentSizeMB} MB");
                result.IsValid = false;
            }
            else
            {
                result.IsValid = errors.Count == 0;
            }

            result.ValidAttachments = validAttachments;
            result.TotalSizeBytes = totalSize;
            result.Errors = errors;

            return result;
        }

        /// <summary>
        /// Validate individual attachment
        /// </summary>
        /// <param name="attachment">Attachment to validate</param>
        /// <returns>Validation result</returns>
        public AttachmentValidationResult ValidateAttachment(EmailAttachment attachment)
        {
            var result = new AttachmentValidationResult();
            var errors = new List<string>();

            try
            {
                // Validate filename
                if (string.IsNullOrWhiteSpace(attachment.FileName))
                {
                    errors.Add("Attachment filename is required");
                }
                else if (attachment.FileName.Length > 255)
                {
                    errors.Add("Attachment filename is too long (max 255 characters)");
                }
                else if (ContainsInvalidFileNameCharacters(attachment.FileName))
                {
                    errors.Add("Attachment filename contains invalid characters");
                }

                // Validate content type
                if (string.IsNullOrWhiteSpace(attachment.ContentType))
                {
                    attachment.ContentType = GetMimeTypeFromFileName(attachment.FileName);
                }

                if (!AllowedMimeTypes.Contains(attachment.ContentType))
                {
                    errors.Add($"Attachment type '{attachment.ContentType}' is not allowed");
                }

                // Validate content
                byte[] contentBytes = Array.Empty<byte>();
                if (!string.IsNullOrWhiteSpace(attachment.Content))
                {
                    try
                    {
                        contentBytes = Convert.FromBase64String(attachment.Content);
                        result.SizeBytes = contentBytes.Length;
                    }
                    catch (FormatException)
                    {
                        errors.Add("Attachment content is not valid Base64");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(attachment.FilePath))
                {
                    if (File.Exists(attachment.FilePath))
                    {
                        var fileInfo = new FileInfo(attachment.FilePath);
                        result.SizeBytes = fileInfo.Length;
                        contentBytes = File.ReadAllBytes(attachment.FilePath);
                    }
                    else
                    {
                        errors.Add($"Attachment file not found: {attachment.FilePath}");
                    }
                }
                else
                {
                    errors.Add("Attachment must have either Content or FilePath");
                }

                // Validate size
                if (result.SizeBytes > MaxAttachmentSizeBytes)
                {
                    errors.Add($"Attachment size ({result.SizeBytes / 1024 / 1024:F1} MB) exceeds limit of {MaxAttachmentSizeMB} MB");
                }

                // Additional security checks
                if (contentBytes.Length > 0)
                {
                    if (IsExecutableFile(contentBytes, attachment.FileName))
                    {
                        errors.Add("Executable files are not allowed as attachments");
                    }
                }

                result.IsValid = errors.Count == 0;
                result.Errors = errors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating attachment {FileName}", attachment.FileName);
                errors.Add($"Validation error: {ex.Message}");
                result.IsValid = false;
                result.Errors = errors;
            }

            return result;
        }

        /// <summary>
        /// Convert file to Base64 string
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Base64 string</returns>
        public async Task<string> ConvertFileToBase64Async(string filePath)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(filePath);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert file to Base64: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Save Base64 content to file
        /// </summary>
        /// <param name="base64Content">Base64 content</param>
        /// <param name="filePath">Target file path</param>
        public async Task SaveBase64ToFileAsync(string base64Content, string filePath)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Content);
                await File.WriteAllBytesAsync(filePath, bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Base64 to file: {FilePath}", filePath);
                throw;
            }
        }

        private static bool ContainsInvalidFileNameCharacters(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return fileName.IndexOfAny(invalidChars) >= 0;
        }

        private static string GetMimeTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".zip" => "application/zip",
                ".json" => "application/json",
                ".xml" => "application/xml",
                _ => "application/octet-stream"
            };
        }

        private static bool IsExecutableFile(byte[] content, string fileName)
        {
            // Check for common executable file signatures
            if (content.Length < 4) return false;

            // Check file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var executableExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".scr", ".pif", ".vbs", ".js" };
            if (executableExtensions.Contains(extension))
                return true;

            // Check PE header (Windows executables)
            if (content.Length >= 2 && content[0] == 0x4D && content[1] == 0x5A) // MZ header
                return true;

            // Check ELF header (Linux executables)
            if (content.Length >= 4 && content[0] == 0x7F && content[1] == 0x45 && content[2] == 0x4C && content[3] == 0x46)
                return true;

            return false;
        }
    }

    /// <summary>
    /// Attachment processing result
    /// </summary>
    public class AttachmentProcessingResult
    {
        public bool IsValid { get; set; }
        public List<EmailAttachment> ValidAttachments { get; set; } = new();
        public long TotalSizeBytes { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Individual attachment validation result
    /// </summary>
    public class AttachmentValidationResult
    {
        public bool IsValid { get; set; }
        public long SizeBytes { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}