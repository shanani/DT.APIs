using DT.EmailWorker.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DT.EmailWorker.Data
{
    /// <summary>
    /// Entity Framework Core context for the Email Worker database
    /// </summary>
    public class EmailDbContext : DbContext
    {
        public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<EmailQueue> EmailQueue { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailHistory> EmailHistory { get; set; }
        public DbSet<EmailAttachment> EmailAttachments { get; set; }
        public DbSet<ProcessingLog> ProcessingLogs { get; set; }
        public DbSet<ServiceStatus> ServiceStatus { get; set; }
        public DbSet<ScheduledEmail> ScheduledEmails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailDbContext).Assembly);

            // Email Queue Configuration
            modelBuilder.Entity<EmailQueue>(entity =>
            {
                entity.ToTable("EmailQueue");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.QueueId)
                    .IsRequired()
                    .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.Priority)
                    .IsRequired()
                    .HasDefaultValue(Models.Enums.EmailPriority.Normal);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasDefaultValue(Models.Enums.EmailQueueStatus.Queued);

                entity.Property(e => e.ToEmails)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Body)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(255);

                // Indexes for performance
                entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt })
                    .HasDatabaseName("IX_EmailQueue_Status_Priority_CreatedAt");

                entity.HasIndex(e => e.ScheduledFor)
                    .HasDatabaseName("IX_EmailQueue_ScheduledFor")
                    .HasFilter("[IsScheduled] = 1");

                entity.HasIndex(e => e.RetryCount)
                    .HasDatabaseName("IX_EmailQueue_RetryCount")
                    .HasFilter("[Status] = 3"); // Failed status

                entity.HasIndex(e => e.QueueId)
                    .IsUnique()
                    .HasDatabaseName("IX_EmailQueue_QueueId");
            });

            // Email Template Configuration
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.ToTable("EmailTemplates");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.SubjectTemplate)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.BodyTemplate)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Unique constraint on active template names
                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_EmailTemplates_Name_Unique")
                    .HasFilter("[IsActive] = 1");
            });

            // Email History Configuration
            modelBuilder.Entity<EmailHistory>(entity =>
            {
                entity.ToTable("EmailHistory");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.QueueId)
                    .IsRequired();

                entity.Property(e => e.ToEmails)
                    .IsRequired();

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.FinalBody)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes for performance
                entity.HasIndex(e => e.SentAt)
                    .HasDatabaseName("IX_EmailHistory_SentAt");

                entity.HasIndex(e => e.TemplateId)
                    .HasDatabaseName("IX_EmailHistory_TemplateId");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_EmailHistory_CreatedAt");

                entity.HasIndex(e => e.QueueId)
                    .HasDatabaseName("IX_EmailHistory_QueueId");
            });

            // Email Attachment Configuration
            modelBuilder.Entity<EmailAttachment>(entity =>
            {
                entity.ToTable("EmailAttachments");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.QueueId)
                    .IsRequired();

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Index for cleanup operations
                entity.HasIndex(e => e.QueueId)
                    .HasDatabaseName("IX_EmailAttachments_QueueId");
            });

            // Processing Log Configuration
            modelBuilder.Entity<ProcessingLog>(entity =>
            {
                entity.ToTable("ProcessingLogs");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LogLevel)
                    .IsRequired();

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Message)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.MachineName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValueSql("HOST_NAME()");

                // Indexes for performance
                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_ProcessingLogs_CreatedAt");

                entity.HasIndex(e => new { e.LogLevel, e.CreatedAt })
                    .HasDatabaseName("IX_ProcessingLogs_LogLevel_CreatedAt");

                entity.HasIndex(e => e.QueueId)
                    .HasDatabaseName("IX_ProcessingLogs_QueueId")
                    .HasFilter("[QueueId] IS NOT NULL");
            });

            // Service Status Configuration
            modelBuilder.Entity<ServiceStatus>(entity =>
            {
                entity.ToTable("ServiceStatus");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ServiceName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.MachineName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValueSql("HOST_NAME()");

                entity.Property(e => e.LastHeartbeat)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.ServiceVersion)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.StartedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Unique constraint on service name and machine name
                entity.HasIndex(e => new { e.ServiceName, e.MachineName })
                    .IsUnique()
                    .HasDatabaseName("IX_ServiceStatus_ServiceName_MachineName");
            });

            // Scheduled Email Configuration
            modelBuilder.Entity<ScheduledEmail>(entity =>
            {
                entity.ToTable("ScheduledEmails");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ScheduleId)
                    .IsRequired()
                    .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ToEmails)
                    .IsRequired();

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Body)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(255);

                // Index for scheduling queries
                entity.HasIndex(e => new { e.NextRunTime, e.IsActive })
                    .HasDatabaseName("IX_ScheduledEmails_NextRunTime_IsActive");

                entity.HasIndex(e => e.ScheduleId)
                    .IsUnique()
                    .HasDatabaseName("IX_ScheduledEmails_ScheduleId");
            });

            // Foreign Key Relationships
            modelBuilder.Entity<EmailQueue>()
                .HasOne(e => e.Template)
                .WithMany(t => t.EmailQueues)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EmailHistory>()
                .HasOne(e => e.Template)
                .WithMany(t => t.EmailHistories)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ScheduledEmail>()
                .HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProcessingLog>()
                .HasOne(e => e.EmailQueue)
                .WithMany(q => q.ProcessingLogs)
                .HasForeignKey(e => e.QueueId)
                .HasPrincipalKey(q => q.QueueId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This will be overridden by DI configuration in Program.cs
                optionsBuilder.UseSqlServer("Server=.;Database=DT_EmailWorker;Integrated Security=true;TrustServerCertificate=True;");
            }
        }
    }
}