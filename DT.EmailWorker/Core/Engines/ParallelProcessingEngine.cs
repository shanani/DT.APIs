using DT.EmailWorker.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DT.EmailWorker.Core.Engines
{
    /// <summary>
    /// Parallel processing engine for handling multiple email operations concurrently
    /// </summary>
    public class ParallelProcessingEngine
    {
        private readonly ILogger<ParallelProcessingEngine> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrency;

        public ParallelProcessingEngine(ILogger<ParallelProcessingEngine> logger, int maxConcurrency = 10)
        {
            _logger = logger;
            _maxConcurrency = maxConcurrency;
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        /// <summary>
        /// Process items in parallel with specified processor function
        /// </summary>
        public async Task<ParallelProcessingResult<T>> ProcessInParallelAsync<T>(
            IEnumerable<T> items,
            Func<T, CancellationToken, Task<ProcessingResult>> processor,
            CancellationToken cancellationToken = default)
        {
            var itemList = items.ToList();
            var result = new ParallelProcessingResult<T>
            {
                TotalItems = itemList.Count,
                StartTime = DateTime.UtcNow
            };

            if (!itemList.Any())
            {
                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger.LogInformation("Starting parallel processing of {Count} items with max concurrency {MaxConcurrency}",
                itemList.Count, _maxConcurrency);

            var stopwatch = Stopwatch.StartNew();
            var successfulItems = new ConcurrentBag<T>();
            var failedItems = new ConcurrentBag<FailedItem<T>>();

            // FIX: Use local variables for Interlocked operations instead of class properties
            int successfulCount = 0;
            int failedCount = 0;

            try
            {
                var tasks = itemList.Select(async item =>
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var processingResult = await processor(item, cancellationToken);

                        if (processingResult.IsSuccess)
                        {
                            successfulItems.Add(item);
                            Interlocked.Increment(ref successfulCount); // FIX: Use local variable
                        }
                        else
                        {
                            failedItems.Add(new FailedItem<T>
                            {
                                Item = item,
                                ErrorMessage = processingResult.ErrorMessage ?? "Unknown error",
                                Exception = processingResult.Exception
                            });
                            Interlocked.Increment(ref failedCount); // FIX: Use local variable
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing item in parallel execution");
                        failedItems.Add(new FailedItem<T>
                        {
                            Item = item,
                            ErrorMessage = ex.Message,
                            Exception = ex
                        });
                        Interlocked.Increment(ref failedCount); // FIX: Use local variable
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                stopwatch.Stop();

                // FIX: Set result properties from local variables
                result.SuccessfulItems = successfulCount;
                result.FailedItems = failedCount;
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.EndTime = DateTime.UtcNow;
                result.IsSuccess = result.FailedItems == 0;
                result.SuccessRate = result.TotalItems > 0 ? (double)result.SuccessfulItems / result.TotalItems * 100 : 0;
                result.ProcessedItems = successfulItems.ToList();
                result.FailedItemDetails = failedItems.ToList();

                _logger.LogInformation("Parallel processing completed in {ElapsedMs}ms. Success: {SuccessCount}/{TotalCount} ({SuccessRate:F1}%)",
                    stopwatch.ElapsedMilliseconds, result.SuccessfulItems, result.TotalItems, result.SuccessRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in parallel processing engine");
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Process items in parallel batches
        /// </summary>
        public async Task<BatchProcessingResult<T>> ProcessInBatchesAsync<T>(
            IEnumerable<T> items,
            Func<IEnumerable<T>, CancellationToken, Task<ProcessingResult>> batchProcessor,
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            var itemList = items.ToList();
            var result = new BatchProcessingResult<T>
            {
                TotalItems = itemList.Count,
                BatchSize = batchSize,
                StartTime = DateTime.UtcNow
            };

            if (!itemList.Any())
            {
                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            var batches = itemList
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            result.TotalBatches = batches.Count;

            _logger.LogInformation("Starting batch processing of {TotalItems} items in {TotalBatches} batches of size {BatchSize}",
                result.TotalItems, result.TotalBatches, batchSize);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var batchTasks = batches.Select(async (batch, batchIndex) =>
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var batchResult = await batchProcessor(batch, cancellationToken);

                        lock (result)
                        {
                            if (batchResult.IsSuccess)
                            {
                                result.SuccessfulBatches++;
                                result.SuccessfulItems += batch.Count;
                            }
                            else
                            {
                                result.FailedBatches++;
                                result.FailedItems += batch.Count;
                                result.BatchErrors.Add(new BatchError
                                {
                                    BatchIndex = batchIndex,
                                    ErrorMessage = batchResult.ErrorMessage ?? "Unknown batch error",
                                    ItemCount = batch.Count
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing batch {BatchIndex}", batchIndex);
                        lock (result)
                        {
                            result.FailedBatches++;
                            result.FailedItems += batch.Count;
                            result.BatchErrors.Add(new BatchError
                            {
                                BatchIndex = batchIndex,
                                ErrorMessage = ex.Message,
                                ItemCount = batch.Count
                            });
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                await Task.WhenAll(batchTasks);

                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.EndTime = DateTime.UtcNow;
                result.IsSuccess = result.FailedBatches == 0;
                result.SuccessRate = result.TotalItems > 0 ? (double)result.SuccessfulItems / result.TotalItems * 100 : 0;

                _logger.LogInformation("Batch processing completed in {ElapsedMs}ms. Success: {SuccessBatches}/{TotalBatches} batches, {SuccessItems}/{TotalItems} items ({SuccessRate:F1}%)",
                    stopwatch.ElapsedMilliseconds, result.SuccessfulBatches, result.TotalBatches, result.SuccessfulItems, result.TotalItems, result.SuccessRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in batch processing engine");
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Get current processing statistics
        /// </summary>
        public ProcessingStatistics GetCurrentStatistics()
        {
            return new ProcessingStatistics
            {
                MaxConcurrency = _maxConcurrency,
                AvailableSlots = _semaphore.CurrentCount,
                ActiveTasks = _maxConcurrency - _semaphore.CurrentCount,
                UtilizationPercentage = ((double)(_maxConcurrency - _semaphore.CurrentCount) / _maxConcurrency) * 100
            };
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }

    /// <summary>
    /// Processing result for individual items
    /// </summary>
    public class ProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Result of parallel processing operation
    /// </summary>
    public class ParallelProcessingResult<T>
    {
        public bool IsSuccess { get; set; }
        public int TotalItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public double SuccessRate { get; set; }
        public long ProcessingTimeMs { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public List<T> ProcessedItems { get; set; } = new();
        public List<FailedItem<T>> FailedItemDetails { get; set; } = new();
    }

    /// <summary>
    /// Result of batch processing operation
    /// </summary>
    public class BatchProcessingResult<T>
    {
        public bool IsSuccess { get; set; }
        public int TotalItems { get; set; }
        public int TotalBatches { get; set; }
        public int BatchSize { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public int SuccessfulBatches { get; set; }
        public int FailedBatches { get; set; }
        public double SuccessRate { get; set; }
        public long ProcessingTimeMs { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public List<BatchError> BatchErrors { get; set; } = new();
    }

    /// <summary>
    /// Failed item details
    /// </summary>
    public class FailedItem<T>
    {
        public T Item { get; set; } = default!;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Batch error details
    /// </summary>
    public class BatchError
    {
        public int BatchIndex { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Current processing statistics
    /// </summary>
    public class ProcessingStatistics
    {
        public int MaxConcurrency { get; set; }
        public int AvailableSlots { get; set; }
        public int ActiveTasks { get; set; }
        public double UtilizationPercentage { get; set; }
    }
}