using Microsoft.EntityFrameworkCore;
using sourcelist.Models; 

namespace sourcelist.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Sourcelist> Sourcelists { get; set; }
        public DbSet<SourcelistDetail> SourcelistDetail { get; set; } 
    }
}