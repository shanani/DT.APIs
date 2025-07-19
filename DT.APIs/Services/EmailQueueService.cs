using DT.APIs.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace DT.APIs.Services
{
    public class EmailQueueService : IEmailQueueService
    {
        private readonly string _connectionString;
        private readonly ILogger<EmailQueueService> _logger;

        public EmailQueueService(IConfiguration configuration, ILogger<EmailQueueService> logger)
        {
            _connectionString = configuration.GetConnectionString("EmailWorkerConn")
                ?? throw new ArgumentNullException("EmailWorkerConn connection string is required");
            _logger = logger;
        }

        public async Task<QueueEmailResponse> QueueEmailAsync(QueueEmailRequest request)
        {
            var queueId = Guid.NewGuid();
            var response = new QueueEmailResponse
            {
                QueueId = queueId,
                QueuedAt = DateTime.UtcNow
            };

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO EmailQueue (
                        QueueId, Priority, Status, ToEmails, CcEmails, BccEmails, 
                        Subject, Body, IsHtml, Attachments, HasEmbeddedImages,
                        ScheduledFor, IsScheduled, CreatedAt, UpdatedAt, 
                        CreatedBy, RequestSource
                    ) VALUES (
                        @QueueId, @Priority, 0, @ToEmails, @CcEmails, @BccEmails,
                        @Subject, @Body, @IsHtml, @Attachments, @HasEmbeddedImages,
                        @ScheduledFor, @IsScheduled, GETUTCDATE(), GETUTCDATE(),
                        @CreatedBy, @RequestSource
                    )", connection);

                command.Parameters.AddWithValue("@QueueId", queueId);
                command.Parameters.AddWithValue("@Priority", request.Priority);
                command.Parameters.AddWithValue("@ToEmails", request.ToEmails);
                command.Parameters.AddWithValue("@CcEmails", (object?)request.CcEmails ?? DBNull.Value);
                command.Parameters.AddWithValue("@BccEmails", (object?)request.BccEmails ?? DBNull.Value);
                command.Parameters.AddWithValue("@Subject", request.Subject);
                command.Parameters.AddWithValue("@Body", request.Body);
                command.Parameters.AddWithValue("@IsHtml", request.IsHtml);
                command.Parameters.AddWithValue("@Attachments",
                    request.Attachments.Any() ? JsonSerializer.Serialize(request.Attachments) : DBNull.Value);
                command.Parameters.AddWithValue("@HasEmbeddedImages",
                    request.Attachments.Any() || request.Body.Contains("data:image"));
                command.Parameters.AddWithValue("@ScheduledFor", (object?)request.ScheduledFor ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsScheduled", request.ScheduledFor.HasValue);
                command.Parameters.AddWithValue("@CreatedBy", (object?)request.CreatedBy ?? "API_USER");
                command.Parameters.AddWithValue("@RequestSource", (object?)request.RequestSource ?? "API");

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                response.Success = true;
                response.Status = request.ScheduledFor.HasValue ? "Scheduled" : "Queued";
                response.Message = "Email successfully queued for processing";
                response.ScheduledFor = request.ScheduledFor;
                response.EstimatedProcessingTime = request.ScheduledFor ?? DateTime.UtcNow.AddMinutes(5);

                _logger.LogInformation("Email queued successfully with ID {QueueId}", queueId);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Failed to queue email: {ex.Message}";
                _logger.LogError(ex, "Error queuing email with ID {QueueId}", queueId);
            }

            return response;
        }

        public async Task<QueueEmailResponse> QueueTemplateEmailAsync(QueueTemplateEmailRequest request)
        {
            var queueId = Guid.NewGuid();
            var response = new QueueEmailResponse
            {
                QueueId = queueId,
                QueuedAt = DateTime.UtcNow
            };

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO EmailQueue (
                        QueueId, Priority, Status, ToEmails, CcEmails, BccEmails, 
                        Subject, Body, IsHtml, TemplateId, TemplateData, RequiresTemplateProcessing,
                        Attachments, HasEmbeddedImages, ScheduledFor, IsScheduled, 
                        CreatedAt, UpdatedAt, CreatedBy, RequestSource
                    ) VALUES (
                        @QueueId, @Priority, 0, @ToEmails, @CcEmails, @BccEmails,
                        'Template Email', '', 1, @TemplateId, @TemplateData, 1,
                        @Attachments, @HasEmbeddedImages, @ScheduledFor, @IsScheduled,
                        GETUTCDATE(), GETUTCDATE(), @CreatedBy, @RequestSource
                    )", connection);

                command.Parameters.AddWithValue("@QueueId", queueId);
                command.Parameters.AddWithValue("@Priority", request.Priority);
                command.Parameters.AddWithValue("@ToEmails", request.ToEmails);
                command.Parameters.AddWithValue("@CcEmails", (object?)request.CcEmails ?? DBNull.Value);
                command.Parameters.AddWithValue("@BccEmails", (object?)request.BccEmails ?? DBNull.Value);
                command.Parameters.AddWithValue("@TemplateId", request.TemplateId);
                command.Parameters.AddWithValue("@TemplateData", JsonSerializer.Serialize(request.TemplateData));
                command.Parameters.AddWithValue("@Attachments",
                    request.Attachments.Any() ? JsonSerializer.Serialize(request.Attachments) : DBNull.Value);
                command.Parameters.AddWithValue("@HasEmbeddedImages", request.Attachments.Any());
                command.Parameters.AddWithValue("@ScheduledFor", (object?)request.ScheduledFor ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsScheduled", request.ScheduledFor.HasValue);
                command.Parameters.AddWithValue("@CreatedBy", (object?)request.CreatedBy ?? "API_USER");
                command.Parameters.AddWithValue("@RequestSource", (object?)request.RequestSource ?? "API_TEMPLATE");

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                response.Success = true;
                response.Status = request.ScheduledFor.HasValue ? "Scheduled" : "Queued";
                response.Message = "Template email successfully queued for processing";
                response.ScheduledFor = request.ScheduledFor;
                response.EstimatedProcessingTime = request.ScheduledFor ?? DateTime.UtcNow.AddMinutes(5);

                _logger.LogInformation("Template email queued successfully with ID {QueueId}, Template ID {TemplateId}", queueId, request.TemplateId);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Failed to queue template email: {ex.Message}";
                _logger.LogError(ex, "Error queuing template email with ID {QueueId}", queueId);
            }

            return response;
        }

        public async Task<BulkQueueEmailResponse> QueueBulkEmailAsync(QueueBulkEmailRequest request)
        {
            var response = new BulkQueueEmailResponse
            {
                TotalQueued = request.Recipients.Count
            };

            var startTime = DateTime.UtcNow;

            try
            {
                if (request.SendIndividually)
                {
                    foreach (var recipient in request.Recipients)
                    {
                        var individualEmail = new QueueEmailRequest
                        {
                            ToEmails = recipient,
                            Subject = request.Subject,
                            Body = request.Body,
                            IsHtml = request.IsHtml,
                            Priority = request.Priority,
                            Attachments = request.Attachments,
                            ScheduledFor = request.ScheduledFor,
                            CreatedBy = request.CreatedBy,
                            RequestSource = request.RequestSource
                        };

                        var result = await QueueEmailAsync(individualEmail);
                        response.Results.Add(result);

                        if (result.Success)
                            response.SuccessfulQueues++;
                        else
                            response.FailedQueues++;
                    }
                }
                else
                {
                    var singleEmail = new QueueEmailRequest
                    {
                        ToEmails = string.Join(",", request.Recipients),
                        Subject = request.Subject,
                        Body = request.Body,
                        IsHtml = request.IsHtml,
                        Priority = request.Priority,
                        Attachments = request.Attachments,
                        ScheduledFor = request.ScheduledFor,
                        CreatedBy = request.CreatedBy,
                        RequestSource = request.RequestSource
                    };

                    var result = await QueueEmailAsync(singleEmail);
                    response.Results.Add(result);

                    if (result.Success)
                        response.SuccessfulQueues = response.TotalQueued;
                    else
                        response.FailedQueues = response.TotalQueued;
                }

                response.SuccessRate = response.TotalQueued > 0 ?
                    (double)response.SuccessfulQueues / response.TotalQueued * 100 : 0;
                response.TotalProcessingTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Bulk email queued: {SuccessfulQueues}/{TotalQueued} successful",
                    response.SuccessfulQueues, response.TotalQueued);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing bulk email");
                throw;
            }

            return response;
        }

        public async Task<EmailStatusResponse?> GetEmailStatusAsync(Guid queueId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT eq.QueueId, eq.Priority, eq.Status, eq.ToEmails, eq.Subject,
                           eq.CreatedAt, eq.ProcessingStartedAt, eq.ProcessedAt, eq.ScheduledFor,
                           eq.RetryCount, eq.ErrorMessage, eq.ProcessedBy, eq.HasEmbeddedImages,
                           CASE WHEN eq.Attachments IS NOT NULL THEN 1 ELSE 0 END as HasAttachments
                    FROM EmailQueue eq
                    WHERE eq.QueueId = @QueueId", connection);

                command.Parameters.AddWithValue("@QueueId", queueId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new EmailStatusResponse
                    {
                        QueueId = reader.GetGuid("QueueId"),
                        Priority = reader.GetInt32("Priority"),
                        Status = GetStatusName(reader.GetInt32("Status")),
                        ToEmails = reader.GetString("ToEmails"),
                        Subject = reader.GetString("Subject"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ProcessingStartedAt = reader.IsDBNull("ProcessingStartedAt") ? null : reader.GetDateTime("ProcessingStartedAt"),
                        ProcessedAt = reader.IsDBNull("ProcessedAt") ? null : reader.GetDateTime("ProcessedAt"),
                        ScheduledFor = reader.IsDBNull("ScheduledFor") ? null : reader.GetDateTime("ScheduledFor"),
                        RetryCount = reader.GetInt32("RetryCount"),
                        ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
                        ProcessedBy = reader.IsDBNull("ProcessedBy") ? null : reader.GetString("ProcessedBy"),
                        HasAttachments = reader.GetInt32("HasAttachments") == 1,
                        HasEmbeddedImages = reader.GetBoolean("HasEmbeddedImages")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email status for {QueueId}", queueId);
                throw;
            }
        }

        public async Task<List<EmailStatusResponse>> GetBatchEmailStatusAsync(List<Guid> queueIds)
        {
            var results = new List<EmailStatusResponse>();

            try
            {
                if (!queueIds.Any()) return results;

                var queueIdParams = string.Join(",", queueIds.Select((_, i) => $"@QueueId{i}"));

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand($@"
                    SELECT eq.QueueId, eq.Priority, eq.Status, eq.ToEmails, eq.Subject,
                           eq.CreatedAt, eq.ProcessingStartedAt, eq.ProcessedAt, eq.ScheduledFor,
                           eq.RetryCount, eq.ErrorMessage, eq.ProcessedBy, eq.HasEmbeddedImages,
                           CASE WHEN eq.Attachments IS NOT NULL THEN 1 ELSE 0 END as HasAttachments
                    FROM EmailQueue eq
                    WHERE eq.QueueId IN ({queueIdParams})", connection);

                for (int i = 0; i < queueIds.Count; i++)
                {
                    command.Parameters.AddWithValue($"@QueueId{i}", queueIds[i]);
                }

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new EmailStatusResponse
                    {
                        QueueId = reader.GetGuid("QueueId"),
                        Priority = reader.GetInt32("Priority"),
                        Status = GetStatusName(reader.GetInt32("Status")),
                        ToEmails = reader.GetString("ToEmails"),
                        Subject = reader.GetString("Subject"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ProcessingStartedAt = reader.IsDBNull("ProcessingStartedAt") ? null : reader.GetDateTime("ProcessingStartedAt"),
                        ProcessedAt = reader.IsDBNull("ProcessedAt") ? null : reader.GetDateTime("ProcessedAt"),
                        ScheduledFor = reader.IsDBNull("ScheduledFor") ? null : reader.GetDateTime("ScheduledFor"),
                        RetryCount = reader.GetInt32("RetryCount"),
                        ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
                        ProcessedBy = reader.IsDBNull("ProcessedBy") ? null : reader.GetString("ProcessedBy"),
                        HasAttachments = reader.GetInt32("HasAttachments") == 1,
                        HasEmbeddedImages = reader.GetBoolean("HasEmbeddedImages")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch email status");
                throw;
            }

            return results;
        }

        public async Task<bool> CancelEmailAsync(Guid queueId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE EmailQueue 
                    SET Status = 4, -- Cancelled
                        UpdatedAt = GETUTCDATE(),
                        ErrorMessage = 'Cancelled by user request'
                    WHERE QueueId = @QueueId 
                      AND Status IN (0, 1)", connection); // Only Queued or Processing

                command.Parameters.AddWithValue("@QueueId", queueId);

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Email cancelled for {QueueId}", queueId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling email {QueueId}", queueId);
                throw;
            }
        }

        public async Task<QueueHealthResponse> GetQueueHealthAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT 
                        COUNT(*) as TotalInQueue,
                        SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) as QueuedEmails,
                        SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as ProcessingEmails,
                        SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) as FailedEmails,
                        SUM(CASE WHEN IsScheduled = 1 AND ScheduledFor > GETUTCDATE() THEN 1 ELSE 0 END) as ScheduledEmails,
                        AVG(CASE WHEN ProcessedAt IS NOT NULL AND ProcessingStartedAt IS NOT NULL 
                            THEN DATEDIFF(SECOND, ProcessingStartedAt, ProcessedAt) ELSE NULL END) as AvgProcessingSeconds,
                        MIN(CASE WHEN Status = 0 THEN DATEDIFF(MINUTE, CreatedAt, GETUTCDATE()) ELSE NULL END) as OldestQueuedMinutes
                    FROM EmailQueue 
                    WHERE Status IN (0, 1, 3) OR (IsScheduled = 1 AND ScheduledFor > GETUTCDATE())", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new QueueHealthResponse
                    {
                        TotalInQueue = reader.GetInt32("TotalInQueue"),
                        QueuedEmails = reader.GetInt32("QueuedEmails"),
                        ProcessingEmails = reader.GetInt32("ProcessingEmails"),
                        FailedEmails = reader.GetInt32("FailedEmails"),
                        ScheduledEmails = reader.GetInt32("ScheduledEmails"),
                        AverageProcessingTimeMinutes = reader.IsDBNull("AvgProcessingSeconds") ? 0 : reader.GetDouble("AvgProcessingSeconds") / 60,
                        OldestQueuedEmailMinutes = reader.IsDBNull("OldestQueuedMinutes") ? null : reader.GetDouble("OldestQueuedMinutes"),
                        WorkerServiceStatus = "Active", // Could check ServiceStatus table
                        LastWorkerActivity = DateTime.UtcNow,
                        HealthStatus = "Healthy"
                    };
                }

                return new QueueHealthResponse { HealthStatus = "Unknown" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue health");
                throw;
            }
        }

        public async Task<QueueStatisticsResponse> GetQueueStatisticsAsync(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.UtcNow.AddDays(-7);
            toDate ??= DateTime.UtcNow;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT 
                        COUNT(*) as TotalProcessed,
                        SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as SuccessfulSent,
                        SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) as Failed,
                        SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) as InQueue,
                        SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as Processing,
                        SUM(CASE WHEN IsScheduled = 1 AND ScheduledFor > GETUTCDATE() THEN 1 ELSE 0 END) as Scheduled,
                        AVG(CASE WHEN ProcessedAt IS NOT NULL AND ProcessingStartedAt IS NOT NULL 
                            THEN DATEDIFF(SECOND, ProcessingStartedAt, ProcessedAt) ELSE NULL END) as AvgProcessingSeconds
                    FROM EmailQueue 
                    WHERE CreatedAt >= @FromDate AND CreatedAt <= @ToDate", connection);

                command.Parameters.AddWithValue("@FromDate", fromDate);
                command.Parameters.AddWithValue("@ToDate", toDate);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var totalProcessed = reader.GetInt32("TotalProcessed");
                    var successfulSent = reader.GetInt32("SuccessfulSent");

                    return new QueueStatisticsResponse
                    {
                        FromDate = fromDate.Value,
                        ToDate = toDate.Value,
                        TotalProcessed = totalProcessed,
                        SuccessfulSent = successfulSent,
                        Failed = reader.GetInt32("Failed"),
                        InQueue = reader.GetInt32("InQueue"),
                        Processing = reader.GetInt32("Processing"),
                        Scheduled = reader.GetInt32("Scheduled"),
                        SuccessRate = totalProcessed > 0 ? (double)successfulSent / totalProcessed * 100 : 0,
                        AverageProcessingTimeSeconds = reader.IsDBNull("AvgProcessingSeconds") ? 0 : reader.GetDouble("AvgProcessingSeconds")
                    };
                }

                return new QueueStatisticsResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue statistics");
                throw;
            }
        }

        public async Task<PagedEmailQueueResponse> GetQueuedEmailsAsync(int page, int pageSize, string? status = null, string? priority = null, DateTime? fromDate = null, DateTime? toDate = null, string? search = null)
        {
            var offset = (page - 1) * pageSize;
            var whereConditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(status))
            {
                whereConditions.Add("eq.Status = @Status");
                parameters.Add(new SqlParameter("@Status", GetStatusId(status)));
            }

            if (!string.IsNullOrEmpty(priority))
            {
                whereConditions.Add("eq.Priority = @Priority");
                parameters.Add(new SqlParameter("@Priority", int.Parse(priority)));
            }

            if (fromDate.HasValue)
            {
                whereConditions.Add("eq.CreatedAt >= @FromDate");
                parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
            }

            if (toDate.HasValue)
            {
                whereConditions.Add("eq.CreatedAt <= @ToDate");
                parameters.Add(new SqlParameter("@ToDate", toDate.Value));
            }

            if (!string.IsNullOrEmpty(search))
            {
                whereConditions.Add("(eq.ToEmails LIKE @Search OR eq.Subject LIKE @Search)");
                parameters.Add(new SqlParameter("@Search", $"%{search}%"));
            }

            var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

            try
            {
                using var connection = new SqlConnection(_connectionString);

                // Get total count
                using var countCommand = new SqlCommand($@"
                    SELECT COUNT(*) 
                    FROM EmailQueue eq 
                    {whereClause}", connection);

                countCommand.Parameters.AddRange(parameters.ToArray());

                await connection.OpenAsync();
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Get paged data
                using var dataCommand = new SqlCommand($@"
                    SELECT eq.QueueId, eq.Priority, eq.Status, eq.ToEmails, eq.Subject,
                           eq.TemplateId, et.Name as TemplateName, eq.CreatedAt, eq.ScheduledFor,
                           eq.ProcessingStartedAt, eq.ProcessedAt, eq.RetryCount, eq.ErrorMessage,
                           CASE WHEN eq.Attachments IS NOT NULL THEN 1 ELSE 0 END as HasAttachments,
                           eq.CreatedBy, eq.RequestSource
                    FROM EmailQueue eq
                    LEFT JOIN EmailTemplates et ON eq.TemplateId = et.Id
                    {whereClause}
                    ORDER BY eq.CreatedAt DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", connection);

                dataCommand.Parameters.AddRange(parameters.ToArray());
                dataCommand.Parameters.Add(new SqlParameter("@Offset", offset));
                dataCommand.Parameters.Add(new SqlParameter("@PageSize", pageSize));

                var items = new List<EmailQueueItem>();
                using var reader = await dataCommand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    items.Add(new EmailQueueItem
                    {
                        QueueId = reader.GetGuid("QueueId"),
                        Priority = reader.GetInt32("Priority"),
                        PriorityName = GetPriorityName(reader.GetInt32("Priority")),
                        Status = reader.GetInt32("Status"),
                        StatusName = GetStatusName(reader.GetInt32("Status")),
                        ToEmails = reader.GetString("ToEmails"),
                        Subject = reader.GetString("Subject"),
                        TemplateId = reader.IsDBNull("TemplateId") ? null : reader.GetInt32("TemplateId"),
                        TemplateName = reader.IsDBNull("TemplateName") ? null : reader.GetString("TemplateName"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ScheduledFor = reader.IsDBNull("ScheduledFor") ? null : reader.GetDateTime("ScheduledFor"),
                        ProcessingStartedAt = reader.IsDBNull("ProcessingStartedAt") ? null : reader.GetDateTime("ProcessingStartedAt"),
                        ProcessedAt = reader.IsDBNull("ProcessedAt") ? null : reader.GetDateTime("ProcessedAt"),
                        RetryCount = reader.GetInt32("RetryCount"),
                        ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
                        HasAttachments = reader.GetInt32("HasAttachments") == 1,
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetString("CreatedBy"),
                        RequestSource = reader.IsDBNull("RequestSource") ? null : reader.GetString("RequestSource")
                    });
                }

                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return new PagedEmailQueueResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1,
                    Items = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queued emails");
                throw;
            }
        }

        private string GetStatusName(int status) => status switch
        {
            0 => "Queued",
            1 => "Processing",
            2 => "Sent",
            3 => "Failed",
            4 => "Cancelled",
            _ => "Unknown"
        };

        private int GetStatusId(string status) => status.ToLower() switch
        {
            "queued" => 0,
            "processing" => 1,
            "sent" => 2,
            "failed" => 3,
            "cancelled" => 4,
            _ => 0
        };

        private string GetPriorityName(int priority) => priority switch
        {
            1 => "Low",
            2 => "Normal",
            3 => "High",
            4 => "Critical",
            _ => "Normal"
        };
    }
}