namespace DT.EmailWorker.Models.Enums
{
    /// <summary>
    /// Priority levels for email processing
    /// </summary>
    public enum EmailPriority
    {
        /// <summary>
        /// Low priority - processed after normal and high priority emails
        /// </summary>
        Low = 1,

        /// <summary>
        /// Normal priority - default priority for most emails
        /// </summary>
        Normal = 2,

        /// <summary>
        /// High priority - processed before normal and low priority emails
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical priority - processed immediately
        /// </summary>
        Critical = 4
    }
}