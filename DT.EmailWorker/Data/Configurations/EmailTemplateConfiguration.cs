using DT.EmailWorker.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT.EmailWorker.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for EmailTemplate entity
    /// </summary>
    public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
    {
        public void Configure(EntityTypeBuilder<EmailTemplate> builder)
        {
            builder.ToTable("EmailTemplates");

            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Basic Properties
            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.Description)
                .HasMaxLength(500);

            builder.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            // Template Content
            builder.Property(e => e.SubjectTemplate)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(e => e.BodyTemplate)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.TemplateData)
                .HasColumnType("nvarchar(max)");

            // Template Settings
            builder.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(e => e.IsSystem)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            // Metadata
            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.UpdatedBy)
                .IsRequired()
                .HasMaxLength(255);

            // Indexes
            builder.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_EmailTemplates_Name_Unique")
                .HasFilter("[IsActive] = 1");

            builder.HasIndex(e => e.Category)
                .HasDatabaseName("IX_EmailTemplates_Category");

            builder.HasIndex(e => new { e.IsActive, e.Category })
                .HasDatabaseName("IX_EmailTemplates_IsActive_Category");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_EmailTemplates_CreatedAt");

            // Navigation Properties
            builder.HasMany(e => e.EmailQueues)
                .WithOne(q => q.Template)
                .HasForeignKey(q => q.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(e => e.EmailHistories)
                .WithOne(h => h.Template)
                .HasForeignKey(h => h.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}