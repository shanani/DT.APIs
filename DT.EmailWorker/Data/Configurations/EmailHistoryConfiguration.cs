using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT.EmailWorker.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for EmailHistory entity
    /// </summary>
    public class EmailHistoryConfiguration : IEntityTypeConfiguration<EmailHistory>
    {
        public void Configure(EntityTypeBuilder<EmailHistory> builder)
        {
            builder.ToTable("EmailHistory");

            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Queue Reference
            builder.Property(e => e.QueueId)
                .IsRequired();

            // Email Details
            builder.Property(e => e.ToEmails)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.CcEmails)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.BccEmails)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(e => e.FinalBody)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            // Processing Results
            builder.Property(e => e.Status)
                .IsRequired()
                .HasConversion<byte>();

            builder.Property(e => e.DeliveryConfirmed)
                .IsRequired()
                .HasDefaultValue(false);

            // Template Info
            builder.Property(e => e.TemplateUsed)
                .HasMaxLength(255);

            // Attachments
            builder.Property(e => e.AttachmentCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.AttachmentMetadata)
                .HasColumnType("nvarchar(max)");

            // Processing Info
            builder.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.ErrorDetails)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.ProcessedBy)
                .HasMaxLength(100);

            // Metadata
            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes for performance and reporting
            builder.HasIndex(e => e.QueueId)
                .HasDatabaseName("IX_EmailHistory_QueueId");

            builder.HasIndex(e => e.SentAt)
                .HasDatabaseName("IX_EmailHistory_SentAt");

            builder.HasIndex(e => e.TemplateId)
                .HasDatabaseName("IX_EmailHistory_TemplateId");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_EmailHistory_CreatedAt");

            builder.HasIndex(e => new { e.Status, e.SentAt })
                .HasDatabaseName("IX_EmailHistory_Status_SentAt");

            builder.HasIndex(e => new { e.CreatedAt, e.Status })
                .HasDatabaseName("IX_EmailHistory_CreatedAt_Status");

            builder.HasIndex(e => e.ProcessedBy)
                .HasDatabaseName("IX_EmailHistory_ProcessedBy");

            // Index for archival operations
            builder.HasIndex(e => new { e.ArchivedAt, e.CreatedAt })
                .HasDatabaseName("IX_EmailHistory_ArchivedAt_CreatedAt");

            // Foreign Key Relationships
            builder.HasOne(e => e.Template)
                .WithMany(t => t.EmailHistories)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}