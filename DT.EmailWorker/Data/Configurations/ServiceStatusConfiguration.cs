using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT.EmailWorker.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for ServiceStatus entity
    /// </summary>
    public class ServiceStatusConfiguration : IEntityTypeConfiguration<ServiceStatus>
    {
        public void Configure(EntityTypeBuilder<ServiceStatus> builder)
        {
            builder.ToTable("ServiceStatus");

            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Service Identification
            builder.Property(e => e.ServiceName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.MachineName)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValueSql("HOST_NAME()");

            // Health Status
            builder.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(ServiceHealthStatus.Healthy)
                .HasConversion<byte>();

            builder.Property(e => e.LastHeartbeat)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Performance Metrics
            builder.Property(e => e.QueueDepth)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.EmailsProcessedPerHour)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.ErrorRate)
                .IsRequired()
                .HasDefaultValue(0.0m)
                .HasColumnType("decimal(5,2)");

            builder.Property(e => e.AverageProcessingTimeMs)
                .HasColumnType("decimal(10,2)");

            // Resource Usage
            builder.Property(e => e.CpuUsagePercent)
                .HasColumnType("decimal(5,2)");

            builder.Property(e => e.MemoryUsageMB)
                .HasColumnType("decimal(10,2)");

            builder.Property(e => e.DiskUsagePercent)
                .HasColumnType("decimal(5,2)");

            // Configuration
            builder.Property(e => e.MaxConcurrentWorkers)
                .IsRequired();

            builder.Property(e => e.CurrentActiveWorkers)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.BatchSize)
                .IsRequired();

            // Version and Status Info
            builder.Property(e => e.ServiceVersion)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.StartedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.LastError)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.LastErrorAt);

            // Statistics
            builder.Property(e => e.TotalEmailsProcessed)
                .IsRequired()
                .HasDefaultValue(0L);

            builder.Property(e => e.TotalEmailsFailed)
                .IsRequired()
                .HasDefaultValue(0L);

            builder.Property(e => e.UptimeSeconds)
                .IsRequired()
                .HasDefaultValue(0L);

            // IMPORTANT: Configure AdditionalInfo property for monitoring
            // Store as JSON string in the database
            builder.Property(e => e.AdditionalInfo)
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? new Dictionary<string, object>() :
                         System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                .HasColumnType("nvarchar(max)")
                .HasColumnName("AdditionalInfoJson")
                .IsRequired(false);

            // Unique constraint on service name and machine name
            builder.HasIndex(e => new { e.ServiceName, e.MachineName })
                .IsUnique()
                .HasDatabaseName("IX_ServiceStatus_ServiceName_MachineName_Unique");

            // Index for monitoring queries
            builder.HasIndex(e => e.LastHeartbeat)
                .HasDatabaseName("IX_ServiceStatus_LastHeartbeat");

            builder.HasIndex(e => e.Status)
                .HasDatabaseName("IX_ServiceStatus_Status");

            builder.HasIndex(e => new { e.Status, e.LastHeartbeat })
                .HasDatabaseName("IX_ServiceStatus_Status_LastHeartbeat");

            // Index for cleanup operations
            builder.HasIndex(e => e.UpdatedAt)
                .HasDatabaseName("IX_ServiceStatus_UpdatedAt");
        }
    }
}