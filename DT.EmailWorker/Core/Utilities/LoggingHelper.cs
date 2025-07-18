using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace DT.EmailWorker.Core.Utilities
{
    /// <summary>
    /// Logging utility helper class
    /// </summary>
    public static class LoggingHelper
    {
        /// <summary>
        /// Log email processing start
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="emailId">Email ID</param>
        /// <param name="recipient">Recipient email</param>
        /// <param name="subject">Email subject</param>
        public static void LogEmailProcessingStart(ILogger logger, int emailId, string recipient, string subject)
        {
            logger.LogInformation("Starting email processing - ID: {EmailId}, To: {Recipient}, Subject: {Subject}",
                emailId, MaskEmail(recipient), TruncateSubject(subject));
        }

        /// <summary>
        /// Log email processing success
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="emailId">Email ID</param>
        /// <param name="recipient">Recipient email</param>
        /// <param name="processingTimeMs">Processing time in milliseconds</param>
        public static void LogEmailProcessingSuccess(ILogger logger, int emailId, string recipient, long processingTimeMs)
        {
            logger.LogInformation("Email sent successfully - ID: {EmailId}, To: {Recipient}, Time: {ProcessingTime}ms",
                emailId, MaskEmail(recipient), processingTimeMs);
        }

        /// <summary>
        /// Log email processing failure
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="emailId">Email ID</param>
        /// <param name="recipient">Recipient email</param>
        /// <param name="exception">Exception that occurred</param>
        /// <param name="retryCount">Current retry count</param>
        public static void LogEmailProcessingFailure(ILogger logger, int emailId, string recipient, Exception exception, int retryCount)
        {
            logger.LogError(exception, "Email processing failed - ID: {EmailId}, To: {Recipient}, Retry: {RetryCount}, Error: {ErrorMessage}",
                emailId, MaskEmail(recipient), retryCount, exception.Message);
        }

        /// <summary>
        /// Log batch processing start
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="batchSize">Batch size</param>
        /// <param name="priority">Priority level</param>
        public static void LogBatchProcessingStart(ILogger logger, int batchSize, string priority)
        {
            logger.LogInformation("Starting batch processing - Size: {BatchSize}, Priority: {Priority}",
                batchSize, priority);
        }

        /// <summary>
        /// Log batch processing completion
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="totalProcessed">Total emails processed</param>
        /// <param name="successCount">Number of successful sends</param>
        /// <param name="failureCount">Number of failures</param>
        /// <param name="totalTimeMs">Total processing time</param>
        public static void LogBatchProcessingComplete(ILogger logger, int totalProcessed, int successCount, int failureCount, long totalTimeMs)
        {
            logger.LogInformation("Batch processing completed - Total: {TotalProcessed}, Success: {SuccessCount}, Failed: {FailureCount}, Time: {TotalTime}ms",
                totalProcessed, successCount, failureCount, totalTimeMs);
        }

        /// <summary>
        /// Log service health status
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="status">Health status</param>
        /// <param name="details">Additional details</param>
        public static void LogServiceHealth(ILogger logger, string serviceName, string status, object? details = null)
        {
            if (details != null)
            {
                var detailsJson = JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = false });
                logger.LogInformation("Service health - Name: {ServiceName}, Status: {Status}, Details: {Details}",
                    serviceName, status, detailsJson);
            }
            else
            {
                logger.LogInformation("Service health - Name: {ServiceName}, Status: {Status}",
                    serviceName, status);
            }
        }

        /// <summary>
        /// Log performance metrics
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="operationName">Operation name</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="additionalMetrics">Additional metrics</param>
        public static void LogPerformanceMetrics(ILogger logger, string operationName, TimeSpan duration, Dictionary<string, object>? additionalMetrics = null)
        {
            if (additionalMetrics?.Any() == true)
            {
                var metricsJson = JsonSerializer.Serialize(additionalMetrics);
                logger.LogInformation("Performance metrics - Operation: {OperationName}, Duration: {Duration}ms, Metrics: {Metrics}",
                    operationName, duration.TotalMilliseconds, metricsJson);
            }
            else
            {
                logger.LogInformation("Performance metrics - Operation: {OperationName}, Duration: {Duration}ms",
                    operationName, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Log database operation
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="operation">Database operation</param>
        /// <param name="entityName">Entity name</param>
        /// <param name="recordCount">Number of records affected</param>
        /// <param name="duration">Operation duration</param>
        public static void LogDatabaseOperation(ILogger logger, string operation, string entityName, int recordCount, TimeSpan duration)
        {
            logger.LogDebug("Database operation - Operation: {Operation}, Entity: {EntityName}, Records: {RecordCount}, Duration: {Duration}ms",
                operation, entityName, recordCount, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Log SMTP operation
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="operation">SMTP operation</param>
        /// <param name="server">SMTP server</param>
        /// <param name="success">Whether operation was successful</param>
        /// <param name="duration">Operation duration</param>
        public static void LogSmtpOperation(ILogger logger, string operation, string server, bool success, TimeSpan duration)
        {
            if (success)
            {
                logger.LogDebug("SMTP operation successful - Operation: {Operation}, Server: {Server}, Duration: {Duration}ms",
                    operation, server, duration.TotalMilliseconds);
            }
            else
            {
                logger.LogWarning("SMTP operation failed - Operation: {Operation}, Server: {Server}, Duration: {Duration}ms",
                    operation, server, duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Log template processing
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="templateName">Template name</param>
        /// <param name="placeholderCount">Number of placeholders processed</param>
        /// <param name="duration">Processing duration</param>
        public static void LogTemplateProcessing(ILogger logger, string templateName, int placeholderCount, TimeSpan duration)
        {
            logger.LogDebug("Template processed - Name: {TemplateName}, Placeholders: {PlaceholderCount}, Duration: {Duration}ms",
                templateName, placeholderCount, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Create structured log scope for email processing
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="emailId">Email ID</param>
        /// <param name="operationType">Operation type</param>
        /// <returns>Disposable scope</returns>
        public static IDisposable CreateEmailProcessingScope(ILogger logger, int emailId, string operationType)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["EmailId"] = emailId,
                ["OperationType"] = operationType,
                ["CorrelationId"] = Guid.NewGuid().ToString()
            });
        }

        /// <summary>
        /// Create structured log scope for batch processing
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="batchId">Batch ID</param>
        /// <param name="batchSize">Batch size</param>
        /// <returns>Disposable scope</returns>
        public static IDisposable CreateBatchProcessingScope(ILogger logger, string batchId, int batchSize)
        {
            return logger.BeginScope(new Dictionary<string, object>
            {
                ["BatchId"] = batchId,
                ["BatchSize"] = batchSize,
                ["CorrelationId"] = Guid.NewGuid().ToString()
            });
        }

        /// <summary>
        /// Time an operation and log the result
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="operationName">Operation name</param>
        /// <param name="operation">Operation to time</param>
        /// <returns>Operation result</returns>
        public static async Task<T> TimeOperationAsync<T>(ILogger logger, string operationName, Func<Task<T>> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await operation();
                stopwatch.Stop();
                LogPerformanceMetrics(logger, operationName, stopwatch.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "Operation failed - Name: {OperationName}, Duration: {Duration}ms, Error: {ErrorMessage}",
                    operationName, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Mask email address for logging (privacy protection)
        /// </summary>
        /// <param name="email">Email address to mask</param>
        /// <returns>Masked email address</returns>
        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "[EMPTY]";

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
                return "[INVALID]";

            var localPart = email.Substring(0, atIndex);
            var domainPart = email.Substring(atIndex);

            if (localPart.Length <= 2)
                return $"**{domainPart}";

            return $"{localPart[0]}***{localPart[^1]}{domainPart}";
        }
        // <summary>
        /// Log worker start
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="workerName">Worker name</param>
        public static void LogWorkerStart(ILogger logger, string workerName)
        {
            logger.LogInformation("{WorkerName} started", workerName);
        }

        /// <summary>
        /// Log worker stop
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="workerName">Worker name</param>
        public static void LogWorkerStop(ILogger logger, string workerName)
        {
            logger.LogInformation("{WorkerName} stopped", workerName);
        }
        /// <summary>
        /// Truncate subject for logging
        /// </summary>
        /// <param name="subject">Email subject</param>
        /// <returns>Truncated subject</returns>
        private static string TruncateSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return "[NO SUBJECT]";

            return subject.Length > 50 ? $"{subject[..47]}..." : subject;
        }
    }
}