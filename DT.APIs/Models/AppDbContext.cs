using System.Data;
using Microsoft.EntityFrameworkCore;

namespace DT.APIs.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> User { get; set; }

        public DbSet<Role> Role { get; set; }

        public DbSet<UserRole> UserRole { get; set; }


        public DbSet<Setting> Setting { get; set; }
    }
}
