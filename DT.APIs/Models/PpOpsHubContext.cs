using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DT.APIs.Models
{
    public partial class PpOpsHubContext : DbContext
    {
        public PpOpsHubContext()
        {
        }

        public PpOpsHubContext(DbContextOptions<PpOpsHubContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Setting> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer("Name=HubDbConn");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.ToTable("Setting");

                entity.Property(e => e.ID)
                    .HasMaxLength(50)
                    .HasColumnName("ID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
