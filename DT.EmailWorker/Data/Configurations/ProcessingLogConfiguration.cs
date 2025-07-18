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

            // Properties
            builder.Property(pl => pl.Level)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            builder.Property(pl => pl.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(pl => pl.Details)
                .HasMaxLength(4000);

            builder.Property(pl => pl.OperationType)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("General");

            builder.Property(pl => pl.EmailId)
                .IsRequired(false);

            builder.Property(pl => pl.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            builder.HasIndex(pl => pl.Level)
                .HasDatabaseName("IX_ProcessingLogs_Level");

            builder.HasIndex(pl => pl.CreatedAt)
                .HasDatabaseName("IX_ProcessingLogs_CreatedAt");

            builder.HasIndex(pl => pl.EmailId)
                .HasDatabaseName("IX_ProcessingLogs_EmailId")
                .HasFilter("[EmailId] IS NOT NULL");

            builder.HasIndex(pl => pl.OperationType)
                .HasDatabaseName("IX_ProcessingLogs_OperationType");

            // Composite index for common queries
            builder.HasIndex(pl => new { pl.Level, pl.CreatedAt })
                .HasDatabaseName("IX_ProcessingLogs_Level_CreatedAt");

            // Foreign Key Relationship (optional - EmailId can be null)
            builder.HasOne<EmailQueue>()
                .WithMany()
                .HasForeignKey(pl => pl.EmailId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        }
    }
}