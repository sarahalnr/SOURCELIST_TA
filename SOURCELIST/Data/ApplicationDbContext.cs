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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sourcelist>(entity =>
            {
              
                entity.ToTable("T_SOURCELIST");
            });

            modelBuilder.Entity<SourcelistDetail>(entity =>
            {
                entity.ToTable("T_SOURCELIST_DETAIL");
            });

        }
    }
}