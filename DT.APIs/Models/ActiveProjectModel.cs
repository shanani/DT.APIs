namespace DT.APIs.Models
{
    /// <summary>
    /// Represents an active project.
    /// </summary>
    public class ActiveProjectModel
    {
        /// <summary>
        /// The unique identifier for the project.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The project code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The English name of the project.
        /// </summary>
        public string EnglishName { get; set; }

        /// <summary>
        /// The Arabic name of the project.
        /// </summary>
        public string ArabicName { get; set; }

        /// <summary>
        /// The URL associated with the project.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The sort order of the project.
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// Indicates if the project is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Tags associated with the project.
        /// </summary>
        public string Tag { get; set; }
    }

}