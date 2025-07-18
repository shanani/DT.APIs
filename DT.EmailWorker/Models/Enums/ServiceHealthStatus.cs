namespace DT.EmailWorker.Models.Enums
{
    /// <summary>
    /// Health status levels for the email worker service
    /// </summary>
    public enum ServiceHealthStatus
    {
        /// <summary>
        /// Service is healthy and operating normally
        /// </summary>
        Healthy = 1,

        /// <summary>
        /// Service has minor issues but is still operational
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Service has critical issues that affect functionality
        /// </summary>
        Critical = 3,

        /// <summary>
        /// Service is offline or unreachable
        /// </summary>
        Offline = 4,
        Unknown = 5
    }
}