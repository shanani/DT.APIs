using DT.EmailWorker.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace DT.EmailWorker.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for ProcessingLog entity
    /// </summary>
    public class ProcessingLogConfiguration : IEntityTypeConfiguration<ProcessingLog>
    {
        public void Configure(EntityTypeBuilder<ProcessingLog> builder)
        {
            builder.ToTable("ProcessingLogs");

            // Primary Key
            builder.HasKey(pl => pl.Id);
            builder.Property(pl => pl.Id).ValueGeneratedOnAdd();

            // Properties - FIX: Use correct property names from ProcessingLog entity
            builder.Property(pl => pl.LogLevel)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            builder.Property(pl => pl.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(pl => pl.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(pl => pl.Exception)
                .HasMaxLength(4000);

            builder.Property(pl => pl.WorkerId)
                .HasMaxLength(100);

            builder.Property(pl => pl.ProcessingStep)
                .HasMaxLength(100);

            builder.Property(pl => pl.ContextData);

            builder.Property(pl => pl.QueueId)
                .IsRequired(false);

            builder.Property(pl => pl.CorrelationId)
                .IsRequired(false);

            builder.Property(pl => pl.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(pl => pl.MachineName)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValueSql("HOST_NAME()");

            // Indexes - FIX: Use correct property names
            builder.HasIndex(pl => pl.LogLevel)
                .HasDatabaseName("IX_ProcessingLogs_LogLevel");

            builder.HasIndex(pl => pl.CreatedAt)
                .HasDatabaseName("IX_ProcessingLogs_CreatedAt");

            builder.HasIndex(pl => pl.QueueId)
                .HasDatabaseName("IX_ProcessingLogs_QueueId")
                .HasFilter("[QueueId] IS NOT NULL");

            builder.HasIndex(pl => pl.Category)
                .HasDatabaseName("IX_ProcessingLogs_Category");

            builder.HasIndex(pl => pl.WorkerId)
                .HasDatabaseName("IX_ProcessingLogs_WorkerId")
                .HasFilter("[WorkerId] IS NOT NULL");

            // Composite index for common queries
            builder.HasIndex(pl => new { pl.LogLevel, pl.CreatedAt })
                .HasDatabaseName("IX_ProcessingLogs_LogLevel_CreatedAt");

            // Foreign Key Relationship (optional - QueueId can be null)
            builder.HasOne(pl => pl.EmailQueue)
                .WithMany()
                .HasForeignKey(pl => pl.QueueId)
                .HasPrincipalKey(eq => eq.QueueId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        }
    }
}