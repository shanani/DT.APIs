using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DT.EmailWorker.Core.Engines
{
    /// <summary>
    /// Content ID (CID) image processor for inline email images
    /// </summary>
    public class CidImageProcessor
    {
        private readonly ILogger<CidImageProcessor> _logger;

        public CidImageProcessor(ILogger<CidImageProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process images and convert to CID references
        /// </summary>
        public async Task<CidProcessingResult> ProcessImagesAsync(string htmlContent, List<ImageAttachment> images)
        {
            var result = new CidProcessingResult
            {
                ProcessedHtml = htmlContent,
                CidMappings = new Dictionary<string, string>()
            };

            if (string.IsNullOrEmpty(htmlContent) || images == null || !images.Any())
            {
                result.IsSuccess = true;
                return result;
            }

            try
            {
                _logger.LogDebug("Processing {Count} images for CID embedding", images.Count);

                var processedHtml = htmlContent;
                var cidCounter = 1;

                foreach (var image in images)
                {
                    var cidId = $"image{cidCounter}@emailworker.local";
                    var cidReference = $"cid:{cidId}";

                    // Replace image sources with CID references
                    if (!string.IsNullOrEmpty(image.OriginalSrc))
                    {
                        // Replace specific src attributes
                        var pattern = $@"src\s*=\s*[""']{Regex.Escape(image.OriginalSrc)}[""']";
                        processedHtml = Regex.Replace(processedHtml, pattern, $"src=\"{cidReference}\"", RegexOptions.IgnoreCase);
                    }
                    else if (!string.IsNullOrEmpty(image.FileName))
                    {
                        // Replace by filename
                        var pattern = $@"src\s*=\s*[""'][^""']*{Regex.Escape(image.FileName)}[""']";
                        processedHtml = Regex.Replace(processedHtml, pattern, $"src=\"{cidReference}\"", RegexOptions.IgnoreCase);
                    }

                    result.CidMappings[cidId] = image.FileName;
                    result.ProcessedImages.Add(new ProcessedCidImage
                    {
                        ContentId = cidId,
                        FileName = image.FileName,
                        ContentType = image.ContentType,
                        Content = image.Content,
                        IsInline = true
                    });

                    cidCounter++;
                }

                result.ProcessedHtml = processedHtml;
                result.IsSuccess = true;

                _logger.LogDebug("CID processing completed successfully. Processed {Count} images", result.ProcessedImages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process CID images");
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Extract image references from HTML
        /// </summary>
        public List<string> ExtractImageReferences(string htmlContent)
        {
            var imageRefs = new List<string>();

            if (string.IsNullOrEmpty(htmlContent))
                return imageRefs;

            try
            {
                // Extract img src attributes
                var imgPattern = @"<img[^>]+src\s*=\s*[""']([^""']+)[""'][^>]*>";
                var matches = Regex.Matches(htmlContent, imgPattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    var src = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(src) && !src.StartsWith("http") && !src.StartsWith("cid:"))
                    {
                        imageRefs.Add(src);
                    }
                }

                // Extract background images from CSS
                var bgPattern = @"background-image\s*:\s*url\s*\(\s*[""']?([^""'\)]+)[""']?\s*\)";
                var bgMatches = Regex.Matches(htmlContent, bgPattern, RegexOptions.IgnoreCase);

                foreach (Match match in bgMatches)
                {
                    var src = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(src) && !src.StartsWith("http") && !src.StartsWith("cid:"))
                    {
                        imageRefs.Add(src);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract image references");
            }

            return imageRefs.Distinct().ToList();
        }

        /// <summary>
        /// Validate image content
        /// </summary>
        public ImageValidationResult ValidateImage(ImageAttachment image)
        {
            var result = new ImageValidationResult
            {
                FileName = image.FileName,
                IsValid = true
            };

            try
            {
                // Validate file name
                if (string.IsNullOrWhiteSpace(image.FileName))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Image file name is required";
                    return result;
                }

                // Validate content type
                if (string.IsNullOrWhiteSpace(image.ContentType) || !IsValidImageContentType(image.ContentType))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid or unsupported image content type: {image.ContentType}";
                    return result;
                }

                // Validate content
                if (string.IsNullOrWhiteSpace(image.Content))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Image content is required";
                    return result;
                }

                // Validate base64 content
                try
                {
                    var bytes = Convert.FromBase64String(image.Content);
                    result.FileSizeBytes = bytes.Length;

                    // Check file size (max 5MB for inline images)
                    const int maxSizeBytes = 5 * 1024 * 1024;
                    if (bytes.Length > maxSizeBytes)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Image size ({FormatFileSize(bytes.Length)}) exceeds maximum allowed size (5MB) for inline images";
                        return result;
                    }

                    // Validate image signature
                    if (!ValidateImageSignature(bytes, image.ContentType))
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "Image content does not match the specified content type";
                        return result;
                    }
                }
                catch (FormatException)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid base64 image content";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Image validation error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Check if content type is valid for images
        /// </summary>
        private bool IsValidImageContentType(string contentType)
        {
            var validTypes = new[]
            {
                "image/jpeg", "image/jpg", "image/png", "image/gif",
                "image/bmp", "image/webp", "image/svg+xml"
            };

            return validTypes.Contains(contentType.ToLowerInvariant());
        }

        /// <summary>
        /// Validate image file signature
        /// </summary>
        private bool ValidateImageSignature(byte[] bytes, string contentType)
        {
            if (bytes.Length < 4) return false;

            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => bytes[0] == 0xFF && bytes[1] == 0xD8,
                "image/png" => bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47,
                "image/gif" => (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46),
                "image/bmp" => bytes[0] == 0x42 && bytes[1] == 0x4D,
                "image/webp" => bytes.Length >= 12 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
                _ => true // For SVG and other formats, skip signature validation
            };
        }

        /// <summary>
        /// Format file size for display
        /// </summary>
        private static string FormatFileSize(long sizeInBytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;

            if (sizeInBytes >= MB)
                return $"{sizeInBytes / (double)MB:F2} MB";
            if (sizeInBytes >= KB)
                return $"{sizeInBytes / (double)KB:F2} KB";

            return $"{sizeInBytes} bytes";
        }
    }

    /// <summary>
    /// Image attachment for CID processing
    /// </summary>
    public class ImageAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // Base64
        public string? OriginalSrc { get; set; }
    }

    /// <summary>
    /// Processed CID image
    /// </summary>
    public class ProcessedCidImage
    {
        public string ContentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsInline { get; set; } = true;
    }

    /// <summary>
    /// CID processing result
    /// </summary>
    public class CidProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string ProcessedHtml { get; set; } = string.Empty;
        public Dictionary<string, string> CidMappings { get; set; } = new();
        public List<ProcessedCidImage> ProcessedImages { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Image validation result
    /// </summary>
    public class ImageValidationResult
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public long FileSizeBytes { get; set; }
    }
}