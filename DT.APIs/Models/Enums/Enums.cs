namespace DT.APIs.Models.Enums
{
    public enum EmailQueueStatus
    {
        Queued = 0,
        Processing = 1,
        Sent = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum EmailPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }
}
