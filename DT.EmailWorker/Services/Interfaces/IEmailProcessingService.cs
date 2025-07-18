namespace DT.EmailWorker.Services.Interfaces
{
    /// <summary>
    /// Core email processing service for queue-based email operations
    /// </summary>
    public interface IEmailProcessingService
    {
        /// <summary>
        /// Process email data for sending (main queue processing method)
        /// </summary>
        /// <param name="emailData">Email data object (EmailProcessingRequest)</param>
        /// <returns>Task</returns>
        Task ProcessEmailAsync(object emailData);
    }
}