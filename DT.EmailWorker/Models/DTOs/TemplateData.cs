namespace DT.EmailWorker.Models.DTOs
{
    /// <summary>
    /// DTO for template data and placeholder replacement
    /// </summary>
    public class TemplateData
    {
        /// <summary>
        /// Template ID
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        /// Template name
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// Subject template with placeholders
        /// </summary>
        public string SubjectTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Body template with placeholders
        /// </summary>
        public string BodyTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Placeholder values for replacement
        /// </summary>
        public Dictionary<string, string> Placeholders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Processed subject (after placeholder replacement)
        /// </summary>
        public string ProcessedSubject { get; set; } = string.Empty;

        /// <summary>
        /// Processed body (after placeholder replacement)
        /// </summary>
        public string ProcessedBody { get; set; } = string.Empty;

        /// <summary>
        /// Whether processing was successful
        /// </summary>
        public bool ProcessingSuccessful { get; set; } = false;

        /// <summary>
        /// Processing error message if any
        /// </summary>
        public string? ProcessingError { get; set; }

        /// <summary>
        /// List of missing placeholders found during processing
        /// </summary>
        public List<string> MissingPlaceholders { get; set; } = new List<string>();

        /// <summary>
        /// Additional template metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}