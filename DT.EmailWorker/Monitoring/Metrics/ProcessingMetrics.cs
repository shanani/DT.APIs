using DT.EmailWorker.Models.Enums;
using System.Collections.Concurrent;

namespace DT.EmailWorker.Monitoring.Metrics
{
    /// <summary>
    /// Processing metrics collector and analyzer
    /// </summary>
    public class ProcessingMetrics
    {
        private readonly ConcurrentDictionary<string, MetricCounter> _counters = new();
        private readonly ConcurrentQueue<ProcessingEvent> _recentEvents = new();
        private readonly object _lockObject = new();
        private DateTime _lastReset = DateTime.UtcNow.AddHours(3);

        /// <summary>
        /// Record email processing event
        /// </summary>
        public void RecordEmailProcessed(bool success, long processingTimeMs, EmailPriority priority = EmailPriority.Normal)
        {
            var eventData = new ProcessingEvent
            {
                Timestamp = DateTime.UtcNow.AddHours(3),
                EventType = success ? EventType.EmailSent : EventType.EmailFailed,
                ProcessingTimeMs = processingTimeMs,
                Priority = priority
            };

            _recentEvents.Enqueue(eventData);

            // Keep only recent events (last 24 hours)
            CleanOldEvents();

            // Update counters
            IncrementCounter("TotalEmails");
            IncrementCounter(success ? "SuccessfulEmails" : "FailedEmails");
            IncrementCounter($"Priority_{priority}");

            UpdateAverageProcessingTime(processingTimeMs);
        }

        /// <summary>
        /// Record batch processing event
        /// </summary>
        public void RecordBatchProcessed(int emailCount, int successCount, int failureCount, long totalProcessingTimeMs)
        {
            var eventData = new ProcessingEvent
            {
                Timestamp = DateTime.UtcNow.AddHours(3),
                EventType = EventType.BatchProcessed,
                ProcessingTimeMs = totalProcessingTimeMs,
                BatchSize = emailCount,
                SuccessCount = successCount,
                FailureCount = failureCount
            };

            _recentEvents.Enqueue(eventData);
            CleanOldEvents();

            IncrementCounter("TotalBatches");
            IncrementCounter("TotalEmails", emailCount);
            IncrementCounter("SuccessfulEmails", successCount);
            IncrementCounter("FailedEmails", failureCount);
        }

        /// <summary>
        /// Record template processing event
        /// </summary>
        public void RecordTemplateProcessed(string templateName, bool success, long processingTimeMs)
        {
            var eventData = new ProcessingEvent
            {
                Timestamp = DateTime.UtcNow.AddHours(3),
                EventType = success ? EventType.TemplateProcessed : EventType.TemplateError,
                ProcessingTimeMs = processingTimeMs,
                TemplateName = templateName
            };

            _recentEvents.Enqueue(eventData);
            CleanOldEvents();

            IncrementCounter($"Template_{templateName}");
            IncrementCounter(success ? "TemplateSuccess" : "TemplateErrors");
        }

        /// <summary>
        /// Record health check event
        /// </summary>
        public void RecordHealthCheck(string checkName, bool healthy, long responseTimeMs)
        {
            var eventData = new ProcessingEvent
            {
                Timestamp = DateTime.UtcNow.AddHours(3),
                EventType = healthy ? EventType.HealthCheckPassed : EventType.HealthCheckFailed,
                ProcessingTimeMs = responseTimeMs,
                HealthCheckName = checkName
            };

            _recentEvents.Enqueue(eventData);
            CleanOldEvents();

            IncrementCounter($"HealthCheck_{checkName}");
            IncrementCounter(healthy ? "HealthChecksPassed" : "HealthChecksFailed");
        }

        /// <summary>
        /// Get current metrics summary
        /// </summary>
        public MetricsSummary GetCurrentMetrics()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow.AddHours(3);
                var last24Hours = _recentEvents.Where(e => e.Timestamp >= now.AddHours(-24)).ToList();
                var lastHour = _recentEvents.Where(e => e.Timestamp >= now.AddHours(-1)).ToList();

                var summary = new MetricsSummary
                {
                    GeneratedAt = now,
                    UptimeSince = _lastReset,

                    // Overall counters
                    TotalEmailsProcessed = GetCounterValue("TotalEmails"),
                    SuccessfulEmails = GetCounterValue("SuccessfulEmails"),
                    FailedEmails = GetCounterValue("FailedEmails"),
                    TotalBatches = GetCounterValue("TotalBatches"),

                    // Last 24 hours
                    Last24Hours = new PeriodMetrics
                    {
                        EmailsProcessed = last24Hours.Count(e => e.EventType == EventType.EmailSent || e.EventType == EventType.EmailFailed),
                        SuccessfulEmails = last24Hours.Count(e => e.EventType == EventType.EmailSent),
                        FailedEmails = last24Hours.Count(e => e.EventType == EventType.EmailFailed),
                        AverageProcessingTimeMs = last24Hours.Where(e => e.ProcessingTimeMs > 0).Average(e => e.ProcessingTimeMs),
                        PeakHourlyRate = CalculatePeakHourlyRate(last24Hours),
                        SuccessRate = CalculateSuccessRate(last24Hours)
                    },

                    // Last hour
                    LastHour = new PeriodMetrics
                    {
                        EmailsProcessed = lastHour.Count(e => e.EventType == EventType.EmailSent || e.EventType == EventType.EmailFailed),
                        SuccessfulEmails = lastHour.Count(e => e.EventType == EventType.EmailSent),
                        FailedEmails = lastHour.Count(e => e.EventType == EventType.EmailFailed),
                        AverageProcessingTimeMs = lastHour.Where(e => e.ProcessingTimeMs > 0).Average(e => e.ProcessingTimeMs),
                        SuccessRate = CalculateSuccessRate(lastHour)
                    },

                    // Priority distribution
                    PriorityDistribution = Enum.GetValues<EmailPriority>()
                        .ToDictionary(p => p, p => GetCounterValue($"Priority_{p}")),

                    // Template usage
                    TopTemplates = last24Hours
                        .Where(e => !string.IsNullOrEmpty(e.TemplateName))
                        .GroupBy(e => e.TemplateName)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .ToDictionary(g => g.Key!, g => g.Count()),

                    // Health check status
                    HealthCheckStatus = _counters
                        .Where(kv => kv.Key.StartsWith("HealthCheck_") && !kv.Key.EndsWith("Passed") && !kv.Key.EndsWith("Failed"))
                        .ToDictionary(
                            kv => kv.Key.Substring("HealthCheck_".Length),
                            kv => new HealthCheckMetric
                            {
                                TotalChecks = kv.Value.Count,
                                PassedChecks = GetCounterValue($"{kv.Key}Passed"),
                                FailedChecks = GetCounterValue($"{kv.Key}Failed"),
                                LastCheckTime = last24Hours
                                    .Where(e => e.HealthCheckName == kv.Key.Substring("HealthCheck_".Length))
                                    .OrderByDescending(e => e.Timestamp)
                                    .FirstOrDefault()?.Timestamp
                            })
                };

                return summary;
            }
        }

        /// <summary>
        /// Reset all metrics
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                _counters.Clear();
                _recentEvents.Clear();
                _lastReset = DateTime.UtcNow.AddHours(3);
            }
        }

        /// <summary>
        /// Get metrics for specific time period
        /// </summary>
        public PeriodMetrics GetMetricsForPeriod(DateTime fromDate, DateTime toDate)
        {
            var periodEvents = _recentEvents
                .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
                .ToList();

            return new PeriodMetrics
            {
                EmailsProcessed = periodEvents.Count(e => e.EventType == EventType.EmailSent || e.EventType == EventType.EmailFailed),
                SuccessfulEmails = periodEvents.Count(e => e.EventType == EventType.EmailSent),
                FailedEmails = periodEvents.Count(e => e.EventType == EventType.EmailFailed),
                AverageProcessingTimeMs = periodEvents.Where(e => e.ProcessingTimeMs > 0).Average(e => e.ProcessingTimeMs),
                SuccessRate = CalculateSuccessRate(periodEvents)
            };
        }

        #region Private Methods

        private void IncrementCounter(string key, int increment = 1)
        {
            _counters.AddOrUpdate(key,
                new MetricCounter { Count = increment, LastUpdated = DateTime.UtcNow.AddHours(3) },
                (k, existing) =>
                {
                    existing.Count += increment;
                    existing.LastUpdated = DateTime.UtcNow.AddHours(3);
                    return existing;
                });
        }

        private int GetCounterValue(string key)
        {
            return _counters.TryGetValue(key, out var counter) ? counter.Count : 0;
        }

        private void UpdateAverageProcessingTime(long processingTimeMs)
        {
            var key = "AverageProcessingTime";
            _counters.AddOrUpdate(key,
                new MetricCounter { Count = 1, Sum = processingTimeMs, LastUpdated = DateTime.UtcNow.AddHours(3) },
                (k, existing) =>
                {
                    existing.Count++;
                    existing.Sum += processingTimeMs;
                    existing.LastUpdated = DateTime.UtcNow.AddHours(3);
                    return existing;
                });
        }

        private void CleanOldEvents()
        {
            var cutoff = DateTime.UtcNow.AddHours(3).AddHours(-24);
            while (_recentEvents.TryPeek(out var oldestEvent) && oldestEvent.Timestamp < cutoff)
            {
                _recentEvents.TryDequeue(out _);
            }
        }

        private double CalculateSuccessRate(List<ProcessingEvent> events)
        {
            var emailEvents = events.Where(e => e.EventType == EventType.EmailSent || e.EventType == EventType.EmailFailed).ToList();
            if (!emailEvents.Any()) return 0;

            var successCount = emailEvents.Count(e => e.EventType == EventType.EmailSent);
            return (double)successCount / emailEvents.Count * 100;
        }

        private int CalculatePeakHourlyRate(List<ProcessingEvent> last24Hours)
        {
            var emailEvents = last24Hours.Where(e => e.EventType == EventType.EmailSent || e.EventType == EventType.EmailFailed);

            return emailEvents
                .GroupBy(e => new { e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour })
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Max();
        }

        #endregion
    }

    #region Data Classes

    public class ProcessingEvent
    {
        public DateTime Timestamp { get; set; }
        public EventType EventType { get; set; }
        public long ProcessingTimeMs { get; set; }
        public EmailPriority Priority { get; set; }
        public string? TemplateName { get; set; }
        public string? HealthCheckName { get; set; }
        public int BatchSize { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    public class MetricCounter
    {
        public int Count { get; set; }
        public long Sum { get; set; }
        public DateTime LastUpdated { get; set; }
        public double Average => Count > 0 ? (double)Sum / Count : 0;
    }

    public class MetricsSummary
    {
        public DateTime GeneratedAt { get; set; }
        public DateTime UptimeSince { get; set; }
        public TimeSpan Uptime => GeneratedAt - UptimeSince;

        public int TotalEmailsProcessed { get; set; }
        public int SuccessfulEmails { get; set; }
        public int FailedEmails { get; set; }
        public int TotalBatches { get; set; }
        public double OverallSuccessRate => TotalEmailsProcessed > 0 ? (double)SuccessfulEmails / TotalEmailsProcessed * 100 : 0;

        public PeriodMetrics Last24Hours { get; set; } = new();
        public PeriodMetrics LastHour { get; set; } = new();

        public Dictionary<EmailPriority, int> PriorityDistribution { get; set; } = new();
        public Dictionary<string, int> TopTemplates { get; set; } = new();
        public Dictionary<string, HealthCheckMetric> HealthCheckStatus { get; set; } = new();
    }

    public class PeriodMetrics
    {
        public int EmailsProcessed { get; set; }
        public int SuccessfulEmails { get; set; }
        public int FailedEmails { get; set; }
        public double AverageProcessingTimeMs { get; set; }
        public double SuccessRate { get; set; }
        public int PeakHourlyRate { get; set; }
    }

    public class HealthCheckMetric
    {
        public int TotalChecks { get; set; }
        public int PassedChecks { get; set; }
        public int FailedChecks { get; set; }
        public DateTime? LastCheckTime { get; set; }
        public double SuccessRate => TotalChecks > 0 ? (double)PassedChecks / TotalChecks * 100 : 0;
    }

    public enum EventType
    {
        EmailSent,
        EmailFailed,
        BatchProcessed,
        TemplateProcessed,
        TemplateError,
        HealthCheckPassed,
        HealthCheckFailed
    }

    #endregion
}