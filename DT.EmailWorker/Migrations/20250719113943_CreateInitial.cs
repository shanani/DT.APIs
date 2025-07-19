using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DT.EmailWorker.Migrations
{
    /// <inheritdoc />
    public partial class CreateInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsInline = table.Column<bool>(type: "bit", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValueSql: "HOST_NAME()"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    QueueDepth = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EmailsProcessedPerHour = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0.0m),
                    AverageProcessingTimeMs = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CpuUsagePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    MemoryUsageMB = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    DiskUsagePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    MaxConcurrentWorkers = table.Column<int>(type: "int", nullable: false),
                    CurrentActiveWorkers = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BatchSize = table.Column<int>(type: "int", nullable: false),
                    ServiceVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalEmailsProcessed = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalEmailsFailed = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    UptimeSeconds = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    AdditionalInfoJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToEmails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CcEmails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BccEmails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FinalBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    TemplateUsed = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AttachmentMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailHistory_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmailQueue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)2),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    ToEmails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CcEmails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BccEmails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHtml = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresTemplateProcessing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Attachments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasEmbeddedImages = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ProcessingStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsScheduled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RequestSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQueue", x => x.Id);
                    table.UniqueConstraint("AK_EmailQueue_QueueId", x => x.QueueId);
                    table.ForeignKey(
                        name: "FK_EmailQueue_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledEmails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    ToEmails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CcEmails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BccEmails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHtml = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExecutionCount = table.Column<int>(type: "int", nullable: false),
                    MaxExecutions = table.Column<int>(type: "int", nullable: true),
                    LastExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastExecutionStatus = table.Column<int>(type: "int", nullable: true),
                    LastExecutionError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledEmails_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProcessingLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogLevel = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessingStep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContextData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValueSql: "HOST_NAME()"),
                    EmailQueueId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingLogs_EmailQueue_EmailQueueId",
                        column: x => x.EmailQueueId,
                        principalTable: "EmailQueue",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProcessingLogs_EmailQueue_QueueId",
                        column: x => x.QueueId,
                        principalTable: "EmailQueue",
                        principalColumn: "QueueId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_ArchivedAt_CreatedAt",
                table: "EmailHistory",
                columns: new[] { "ArchivedAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_CreatedAt",
                table: "EmailHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_CreatedAt_Status",
                table: "EmailHistory",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_ProcessedBy",
                table: "EmailHistory",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_QueueId",
                table: "EmailHistory",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_SentAt",
                table: "EmailHistory",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_Status_SentAt",
                table: "EmailHistory",
                columns: new[] { "Status", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_TemplateId",
                table: "EmailHistory",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_CreatedAt_Status",
                table: "EmailQueue",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_ProcessingStartedAt",
                table: "EmailQueue",
                column: "ProcessingStartedAt",
                filter: "[ProcessingStartedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_QueueId_Unique",
                table: "EmailQueue",
                column: "QueueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_RetryCount",
                table: "EmailQueue",
                column: "RetryCount",
                filter: "[Status] = 3");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_ScheduledFor",
                table: "EmailQueue",
                column: "ScheduledFor",
                filter: "[IsScheduled] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_Status_Priority_CreatedAt",
                table: "EmailQueue",
                columns: new[] { "Status", "Priority", "CreatedAt" })
                .Annotation("SqlServer:Include", new[] { "QueueId", "ToEmails", "Subject" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_TemplateId",
                table: "EmailQueue",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Category",
                table: "EmailTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CreatedAt",
                table: "EmailTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_IsActive_Category",
                table: "EmailTemplates",
                columns: new[] { "IsActive", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_Unique",
                table: "EmailTemplates",
                column: "Name",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingLogs_Category",
                table: "ProcessingLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingLogs_CreatedAt",
                table: "ProcessingLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingLogs_EmailQueueId",
                table: "ProcessingLogs",
                column: "EmailQueueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingLogs_LogLevel",
                table: "ProcessingLogs",
                column: "LogLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingLogs_LogLevel_CreatedAt",
                table: "ProcessingLogs",
                columns: new[] { "LogLevel", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingLogs_QueueId",
                table: "ProcessingLogs",
                column: "QueueId",
                filter: "[QueueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingLogs_WorkerId",
                table: "ProcessingLogs",
                column: "WorkerId",
                filter: "[WorkerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledEmails_TemplateId",
                table: "ScheduledEmails",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatus_LastHeartbeat",
                table: "ServiceStatus",
                column: "LastHeartbeat");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatus_ServiceName_MachineName_Unique",
                table: "ServiceStatus",
                columns: new[] { "ServiceName", "MachineName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatus_Status",
                table: "ServiceStatus",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatus_Status_LastHeartbeat",
                table: "ServiceStatus",
                columns: new[] { "Status", "LastHeartbeat" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatus_UpdatedAt",
                table: "ServiceStatus",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "EmailHistory");

            migrationBuilder.DropTable(
                name: "ProcessingLogs");

            migrationBuilder.DropTable(
                name: "ScheduledEmails");

            migrationBuilder.DropTable(
                name: "ServiceStatus");

            migrationBuilder.DropTable(
                name: "EmailQueue");

            migrationBuilder.DropTable(
                name: "EmailTemplates");
        }
    }
}
