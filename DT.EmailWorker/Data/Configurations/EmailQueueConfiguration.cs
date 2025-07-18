using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT.EmailWorker.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for EmailQueue entity
    /// </summary>
    public class EmailQueueConfiguration : IEntityTypeConfiguration<EmailQueue>
    {
        public void Configure(EntityTypeBuilder<EmailQueue> builder)
        {
            builder.ToTable("EmailQueue");

            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Unique Queue ID
            builder.Property(e => e.QueueId)
                .IsRequired()
                .HasDefaultValueSql("NEWID()");

            // Enum Properties
            builder.Property(e => e.Priority)
                .IsRequired()
                .HasDefaultValue(EmailPriority.Normal)
                .HasConversion<byte>();

            builder.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(EmailQueueStatus.Queued)
                .HasConversion<byte>();

            // Email Content
            builder.Property(e => e.ToEmails)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(e => e.CcEmails)
                .HasMaxLength(4000);

            builder.Property(e => e.BccEmails)
                .HasMaxLength(4000);

            builder.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(e => e.Body)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.IsHtml)
                .IsRequired()
                .HasDefaultValue(true);

            // Template Properties
            builder.Property(e => e.TemplateData)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.RequiresTemplateProcessing)
                .IsRequired()
                .HasDefaultValue(false);

            // Attachment Properties
            builder.Property(e => e.Attachments)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.HasEmbeddedImages)
                .IsRequired()
                .HasDefaultValue(false);

            // Processing Properties
            builder.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.ErrorMessage)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.ProcessedBy)
                .HasMaxLength(100);

            // Scheduling Properties
            builder.Property(e => e.IsScheduled)
                .IsRequired()
                .HasDefaultValue(false);

            // Metadata
            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.RequestSource)
                .HasMaxLength(100);

            // Indexes for performance optimization
            builder.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt })
                .HasDatabaseName("IX_EmailQueue_Status_Priority_CreatedAt")
                .IncludeProperties(e => new { e.QueueId, e.ToEmails, e.Subject });

            builder.HasIndex(e => e.ScheduledFor)
                .HasDatabaseName("IX_EmailQueue_ScheduledFor")
                .HasFilter("[IsScheduled] = 1");

            builder.HasIndex(e => e.RetryCount)
                .HasDatabaseName("IX_EmailQueue_RetryCount")
                .HasFilter("[Status] = 3"); // Failed status

            builder.HasIndex(e => e.QueueId)
                .IsUnique()
                .HasDatabaseName("IX_EmailQueue_QueueId_Unique");

            builder.HasIndex(e => e.ProcessingStartedAt)
                .HasDatabaseName("IX_EmailQueue_ProcessingStartedAt")
                .HasFilter("[ProcessingStartedAt] IS NOT NULL");

            builder.HasIndex(e => new { e.CreatedAt, e.Status })
                .HasDatabaseName("IX_EmailQueue_CreatedAt_Status");

            // Foreign Key Relationships
            builder.HasOne(e => e.Template)
                .WithMany(t => t.EmailQueues)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(e => e.ProcessingLogs)
                .WithOne(p => p.EmailQueue)
                .HasForeignKey(p => p.QueueId)
                .HasPrincipalKey(e => e.QueueId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}