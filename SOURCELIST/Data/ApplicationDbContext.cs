using Microsoft.EntityFrameworkCore;
using sourcelist.Models; // Baris ini sekarang akan berfungsi dengan benar

namespace sourcelist.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User> MUsers { get; set; }
        public DbSet<Supplier> MSuppliers { get; set; }
        public DbSet<Sourcelist> TSourcelists { get; set; }
        public DbSet<SourcelistDetail> TSourcelistDetails { get; set; } 
    }
}