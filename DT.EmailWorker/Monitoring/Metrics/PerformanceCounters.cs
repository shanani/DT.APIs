using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DT.EmailWorker.Monitoring.Metrics
{
    /// <summary>
    /// System performance counters for monitoring resource usage
    /// </summary>
    public class PerformanceCounters : IDisposable
    {
        private readonly ILogger<PerformanceCounters> _logger;
        private readonly Process _currentProcess;
        private readonly Timer _monitoringTimer;
        private readonly object _lockObject = new();

        private PerformanceData _currentData = new();
        private readonly Queue<PerformanceData> _historicalData = new();
        private const int MaxHistoricalRecords = 1440; // 24 hours of minute-by-minute data

        public PerformanceCounters(ILogger<PerformanceCounters> logger)
        {
            _logger = logger;
            _currentProcess = Process.GetCurrentProcess();

            // Start monitoring timer (every minute)
            _monitoringTimer = new Timer(CollectPerformanceData, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Get current performance snapshot
        /// </summary>
        public PerformanceData GetCurrentPerformance()
        {
            lock (_lockObject)
            {
                CollectCurrentData();
                return _currentData.Clone();
            }
        }

        /// <summary>
        /// Get performance data for specified period
        /// </summary>
        public PerformanceReport GetPerformanceReport(TimeSpan period)
        {
            lock (_lockObject)
            {
                var cutoff = DateTime.UtcNow.AddHours(3) - period;
                var relevantData = _historicalData
                    .Where(d => d.Timestamp >= cutoff)
                    .ToList();

                if (!relevantData.Any())
                {
                    return new PerformanceReport { Period = period, NoDataAvailable = true };
                }

                return new PerformanceReport
                {
                    Period = period,
                    DataPoints = relevantData.Count,

                    // CPU metrics
                    AverageCpuUsage = relevantData.Average(d => d.CpuUsagePercent),
                    MaxCpuUsage = relevantData.Max(d => d.CpuUsagePercent),
                    MinCpuUsage = relevantData.Min(d => d.CpuUsagePercent),

                    // Memory metrics
                    AverageMemoryUsageMB = relevantData.Average(d => d.WorkingSetMB),
                    MaxMemoryUsageMB = relevantData.Max(d => d.WorkingSetMB),
                    MinMemoryUsageMB = relevantData.Min(d => d.WorkingSetMB),
                    AveragePrivateMemoryMB = relevantData.Average(d => d.PrivateMemoryMB),

                    // Thread metrics
                    AverageThreadCount = relevantData.Average(d => d.ThreadCount),
                    MaxThreadCount = relevantData.Max(d => d.ThreadCount),
                    MinThreadCount = relevantData.Min(d => d.ThreadCount),

                    // Handle metrics
                    AverageHandleCount = relevantData.Average(d => d.HandleCount),
                    MaxHandleCount = relevantData.Max(d => d.HandleCount),

                    // GC metrics
                    TotalGCCollections = relevantData.Sum(d => d.GCCollections),
                    AverageGCMemoryMB = relevantData.Average(d => d.GCTotalMemoryMB),

                    StartTime = relevantData.First().Timestamp,
                    EndTime = relevantData.Last().Timestamp
                };
            }
        }

        /// <summary>
        /// Get system health status based on thresholds
        /// </summary>
        public SystemHealthStatus GetSystemHealthStatus()
        {
            var current = GetCurrentPerformance();
            var status = new SystemHealthStatus
            {
                Timestamp = current.Timestamp,
                OverallHealth = HealthLevel.Healthy
            };

            // CPU health check
            status.CpuHealth = current.CpuUsagePercent switch
            {
                < 70 => HealthLevel.Healthy,
                < 85 => HealthLevel.Warning,
                _ => HealthLevel.Critical
            };

            // Memory health check (using working set)
            var memoryUsagePercent = (current.WorkingSetMB / GetTotalSystemMemoryMB()) * 100;
            status.MemoryHealth = memoryUsagePercent switch
            {
                < 70 => HealthLevel.Healthy,
                < 85 => HealthLevel.Warning,
                _ => HealthLevel.Critical
            };

            // Thread health check
            status.ThreadHealth = current.ThreadCount switch
            {
                < 100 => HealthLevel.Healthy,
                < 200 => HealthLevel.Warning,
                _ => HealthLevel.Critical
            };

            // Handle health check
            status.HandleHealth = current.HandleCount switch
            {
                < 5000 => HealthLevel.Healthy,
                < 8000 => HealthLevel.Warning,
                _ => HealthLevel.Critical
            };

            // Determine overall health (worst of all components)
            var healthLevels = new[] { status.CpuHealth, status.MemoryHealth, status.ThreadHealth, status.HandleHealth };
            status.OverallHealth = healthLevels.Max();

            // Add warnings and recommendations
            if (status.CpuHealth >= HealthLevel.Warning)
                status.Warnings.Add($"High CPU usage: {current.CpuUsagePercent:F1}%");
            if (status.MemoryHealth >= HealthLevel.Warning)
                status.Warnings.Add($"High memory usage: {current.WorkingSetMB:F0} MB");
            if (status.ThreadHealth >= HealthLevel.Warning)
                status.Warnings.Add($"High thread count: {current.ThreadCount}");
            if (status.HandleHealth >= HealthLevel.Warning)
                status.Warnings.Add($"High handle count: {current.HandleCount}");

            return status;
        }

        /// <summary>
        /// Get performance trends over time
        /// </summary>
        public PerformanceTrends GetPerformanceTrends(TimeSpan period)
        {
            lock (_lockObject)
            {
                var cutoff = DateTime.UtcNow.AddHours(3) - period;
                var relevantData = _historicalData
                    .Where(d => d.Timestamp >= cutoff)
                    .OrderBy(d => d.Timestamp)
                    .ToList();

                if (relevantData.Count < 2)
                {
                    return new PerformanceTrends { InsufficientData = true };
                }

                return new PerformanceTrends
                {
                    Period = period,
                    CpuTrend = CalculateTrend(relevantData.Select(d => d.CpuUsagePercent).ToList()),
                    MemoryTrend = CalculateTrend(relevantData.Select(d => d.WorkingSetMB).ToList()),
                    ThreadTrend = CalculateTrend(relevantData.Select(d => (double)d.ThreadCount).ToList()),
                    HandleTrend = CalculateTrend(relevantData.Select(d => (double)d.HandleCount).ToList()),
                    DataPointCount = relevantData.Count
                };
            }
        }

        #region Private Methods

        private void CollectPerformanceData(object? state)
        {
            try
            {
                lock (_lockObject)
                {
                    CollectCurrentData();

                    // Add to historical data
                    _historicalData.Enqueue(_currentData.Clone());

                    // Remove old data
                    while (_historicalData.Count > MaxHistoricalRecords)
                    {
                        _historicalData.Dequeue();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting performance data");
            }
        }

        private void CollectCurrentData()
        {
            try
            {
                _currentProcess.Refresh();

                _currentData = new PerformanceData
                {
                    Timestamp = DateTime.UtcNow.AddHours(3),

                    // CPU
                    CpuUsagePercent = GetCpuUsage(),

                    // Memory
                    WorkingSetMB = _currentProcess.WorkingSet64 / 1024.0 / 1024.0,
                    PrivateMemoryMB = _currentProcess.PrivateMemorySize64 / 1024.0 / 1024.0,
                    VirtualMemoryMB = _currentProcess.VirtualMemorySize64 / 1024.0 / 1024.0,

                    // Threads and Handles
                    ThreadCount = _currentProcess.Threads.Count,
                    HandleCount = _currentProcess.HandleCount,

                    // GC
                    GCTotalMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                    GCCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),

                    // Process info
                    ProcessStartTime = _currentProcess.StartTime,
                    TotalProcessorTime = _currentProcess.TotalProcessorTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting current performance data");
            }
        }

        private double GetCpuUsage()
        {
            try
            {
                // This is a simplified CPU usage calculation
                // For production, consider using PerformanceCounter for more accurate results
                var startTime = DateTime.UtcNow.AddHours(3);
                var startCpuUsage = _currentProcess.TotalProcessorTime;

                Thread.Sleep(100); // Small delay for measurement

                _currentProcess.Refresh();
                var endTime = DateTime.UtcNow.AddHours(3);
                var endCpuUsage = _currentProcess.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0; // Return 0 if unable to calculate
            }
        }

        private double GetTotalSystemMemoryMB()
        {
            // Simplified - in production you might want to use WMI or other methods
            return 8192; // Assume 8GB as default
        }

        private TrendDirection CalculateTrend(List<double> values)
        {
            if (values.Count < 2) return TrendDirection.Stable;

            var firstHalf = values.Take(values.Count / 2).Average();
            var secondHalf = values.Skip(values.Count / 2).Average();

            var percentageChange = Math.Abs((secondHalf - firstHalf) / firstHalf * 100);

            if (percentageChange < 5) return TrendDirection.Stable;

            return secondHalf > firstHalf ? TrendDirection.Increasing : TrendDirection.Decreasing;
        }

        #endregion

        public void Dispose()
        {
            _monitoringTimer?.Dispose();
            _currentProcess?.Dispose();
        }
    }

    #region Data Classes

    public class PerformanceData
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public double WorkingSetMB { get; set; }
        public double PrivateMemoryMB { get; set; }
        public double VirtualMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public double GCTotalMemoryMB { get; set; }
        public int GCCollections { get; set; }
        public DateTime ProcessStartTime { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }

        public PerformanceData Clone()
        {
            return new PerformanceData
            {
                Timestamp = Timestamp,
                CpuUsagePercent = CpuUsagePercent,
                WorkingSetMB = WorkingSetMB,
                PrivateMemoryMB = PrivateMemoryMB,
                VirtualMemoryMB = VirtualMemoryMB,
                ThreadCount = ThreadCount,
                HandleCount = HandleCount,
                GCTotalMemoryMB = GCTotalMemoryMB,
                GCCollections = GCCollections,
                ProcessStartTime = ProcessStartTime,
                TotalProcessorTime = TotalProcessorTime
            };
        }
    }

    public class PerformanceReport
    {
        public TimeSpan Period { get; set; }
        public int DataPoints { get; set; }
        public bool NoDataAvailable { get; set; }

        public double AverageCpuUsage { get; set; }
        public double MaxCpuUsage { get; set; }
        public double MinCpuUsage { get; set; }

        public double AverageMemoryUsageMB { get; set; }
        public double MaxMemoryUsageMB { get; set; }
        public double MinMemoryUsageMB { get; set; }
        public double AveragePrivateMemoryMB { get; set; }

        public double AverageThreadCount { get; set; }
        public int MaxThreadCount { get; set; }
        public int MinThreadCount { get; set; }

        public double AverageHandleCount { get; set; }
        public int MaxHandleCount { get; set; }

        public int TotalGCCollections { get; set; }
        public double AverageGCMemoryMB { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class SystemHealthStatus
    {
        public DateTime Timestamp { get; set; }
        public HealthLevel OverallHealth { get; set; }
        public HealthLevel CpuHealth { get; set; }
        public HealthLevel MemoryHealth { get; set; }
        public HealthLevel ThreadHealth { get; set; }
        public HealthLevel HandleHealth { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class PerformanceTrends
    {
        public TimeSpan Period { get; set; }
        public bool InsufficientData { get; set; }
        public TrendDirection CpuTrend { get; set; }
        public TrendDirection MemoryTrend { get; set; }
        public TrendDirection ThreadTrend { get; set; }
        public TrendDirection HandleTrend { get; set; }
        public int DataPointCount { get; set; }
    }

    public enum HealthLevel
    {
        Healthy = 0,
        Warning = 1,
        Critical = 2
    }

    public enum TrendDirection
    {
        Decreasing = -1,
        Stable = 0,
        Increasing = 1
    }

    #endregion
}