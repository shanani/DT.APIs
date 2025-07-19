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