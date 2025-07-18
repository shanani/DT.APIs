namespace DT.EmailWorker.Models.Enums
{
    /// <summary>
    /// Overall processing status for the email worker service
    /// </summary>
    public enum ProcessingStatus
    {
        /// <summary>
        /// Service is idle and not processing
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Service is actively processing emails
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Service is paused
        /// </summary>
        Paused = 2,

        /// <summary>
        /// Service encountered an error
        /// </summary>
        Error = 3,

        /// <summary>
        /// Service is stopping
        /// </summary>
        Stopping = 4,

        /// <summary>
        /// Service is starting up
        /// </summary>
        Starting = 5
    }
}