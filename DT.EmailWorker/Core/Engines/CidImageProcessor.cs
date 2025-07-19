using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DT.EmailWorker.Core.Engines
{
    /// <summary>
    /// Content ID (CID) image processor for inline email images
    /// Converts base64 inline images to CID attachments for better email client compatibility
    /// </summary>
    public class CidImageProcessor
    {
        private readonly ILogger<CidImageProcessor> _logger;

        // Regex patterns for detecting different image sources
        private static readonly Regex Base64ImageRegex = new(
            @"src\s*=\s*[""']data:image\/([^;]+);base64,([^""']+)[""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex BackgroundImageRegex = new(
            @"background(?:-image)?\s*:\s*url\s*\(\s*[""']?data:image\/([^;]+);base64,([^)""']+)[""']?\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public CidImageProcessor(ILogger<CidImageProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process HTML content and convert all base64 images to CID references
        /// This is the main entry point for converting inline base64 images
        /// </summary>
        public async Task<CidProcessingResult> ProcessImagesAsync(string htmlContent, List<ImageAttachment>? existingImages = null)
        {
            var result = new CidProcessingResult
            {
                ProcessedHtml = htmlContent,
                CidMappings = new Dictionary<string, string>()
            };

            if (string.IsNullOrEmpty(htmlContent))
            {
                result.IsSuccess = true;
                return result;
            }

            try
            {
                _logger.LogDebug("Starting CID image processing for HTML content");

                // Step 1: Extract all base64 images from HTML
                var extractedImages = ExtractBase64Images(htmlContent);

                // Step 2: Combine with existing images if provided
                var allImages = new List<ImageAttachment>();
                if (existingImages?.Any() == true)
                {
                    allImages.AddRange(existingImages);
                }
                allImages.AddRange(extractedImages);

                if (!allImages.Any())
                {
                    _logger.LogDebug("No images found to process");
                    result.IsSuccess = true;
                    return result;
                }

                _logger.LogDebug("Processing {Count} images for CID conversion", allImages.Count);

                // Step 3: Convert each image to CID
                var processedHtml = htmlContent;
                var cidCounter = 1;

                foreach (var image in allImages)
                {
                    // Validate image before processing
                    var validation = ValidateImage(image);
                    if (!validation.IsValid)
                    {
                        _logger.LogWarning("Skipping invalid image {FileName}: {Error}",
                            image.FileName, validation.ErrorMessage);
                        continue;
                    }

                    // Generate unique CID
                    var cidId = $"image{cidCounter}@emailworker.local";
                    var cidReference = $"cid:{cidId}";

                    // Replace base64 sources with CID references
                    processedHtml = ReplaceImageWithCid(processedHtml, image, cidReference);

                    // Track the mapping
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

                _logger.LogDebug("CID processing completed successfully. Processed {Count} images",
                    result.ProcessedImages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process images for CID conversion");
                result.IsSuccess = false;
                result.ErrorMessage = $"CID processing failed: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Extract all base64 images from HTML content
        /// Supports both img src and CSS background-image properties
        /// </summary>
        public List<ImageAttachment> ExtractBase64Images(string htmlContent)
        {
            var images = new List<ImageAttachment>();

            if (string.IsNullOrEmpty(htmlContent))
                return images;

            try
            {
                // Extract from img src attributes
                var imgMatches = Base64ImageRegex.Matches(htmlContent);
                foreach (Match match in imgMatches)
                {
                    var contentType = $"image/{match.Groups[1].Value}";
                    var base64Content = match.Groups[2].Value;
                    var fileName = GenerateFileName(contentType);

                    images.Add(new ImageAttachment
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        Content = base64Content,
                        OriginalSrc = match.Value
                    });
                }

                // Extract from CSS background-image properties
                var bgMatches = BackgroundImageRegex.Matches(htmlContent);
                foreach (Match match in bgMatches)
                {
                    var contentType = $"image/{match.Groups[1].Value}";
                    var base64Content = match.Groups[2].Value;
                    var fileName = GenerateFileName(contentType);

                    images.Add(new ImageAttachment
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        Content = base64Content,
                        OriginalSrc = match.Value
                    });
                }

                _logger.LogDebug("Extracted {Count} base64 images from HTML", images.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract base64 images from HTML");
            }

            return images;
        }

        /// <summary>
        /// Replace image references in HTML with CID references
        /// CRITICAL FIX: Replace only the specific image, not all images
        /// </summary>
        private string ReplaceImageWithCid(string htmlContent, ImageAttachment image, string cidReference)
        {
            var processedHtml = htmlContent;

            try
            {
                // 🚀 CRITICAL FIX: Replace only the EXACT base64 content for this specific image
                if (!string.IsNullOrEmpty(image.OriginalSrc))
                {
                    // Replace the exact match only
                    processedHtml = processedHtml.Replace(image.OriginalSrc, $"src=\"{cidReference}\"");
                }
                else if (!string.IsNullOrEmpty(image.Content))
                {
                    // Replace the specific base64 content for this image only
                    var specificBase64Pattern = $@"src\s*=\s*[""']data:image\/[^;]+;base64,{Regex.Escape(image.Content)}[""']";
                    processedHtml = Regex.Replace(processedHtml, specificBase64Pattern, $"src=\"{cidReference}\"",
                        RegexOptions.IgnoreCase);

                    // Also handle background images with this specific base64
                    var specificBgPattern = $@"background(?:-image)?\s*:\s*url\s*\(\s*[""']?data:image\/[^;]+;base64,{Regex.Escape(image.Content)}[""']?\s*\)";
                    processedHtml = Regex.Replace(processedHtml, specificBgPattern, $"background-image: url({cidReference})",
                        RegexOptions.IgnoreCase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to replace image {FileName} with CID reference", image.FileName);
            }

            return processedHtml;
        }

        /// <summary>
        /// Validate image attachment for CID processing
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
        /// Generate appropriate filename for image based on content type
        /// </summary>
        private string GenerateFileName(string contentType)
        {
            var extension = contentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => "jpg",
                "image/png" => "png",
                "image/gif" => "gif",
                "image/bmp" => "bmp",
                "image/webp" => "webp",
                "image/svg+xml" => "svg",
                _ => "img"
            };

            return $"inline_image_{Guid.NewGuid():N}.{extension}";
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
        /// Validate image file signature against content type
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
    /// Processed CID image result
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
    /// CID processing result container
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