using Microsoft.EntityFrameworkCore;
using DAL.Entities;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }
        public AppDbContext()
        {
        }
        public DbSet<Citizencategories> citizencategories { get; set; }
        public DbSet<Users> users { get; set; }
        public DbSet<Categories> categories { get; set; }
        public DbSet<Services> services { get; set; }
        public DbSet<Resources> resources { get; set; }
        public DbSet<Orders> orders { get; set; }
        public DbSet<Servicecategories> servicecategories { get; set; }
        public DbSet<Orderresources> orderresources { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Host=localhost;Port=5432;Database=entertainment center;Username=postgres;Password=124360AVqwreyp";
            optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Servicecategories>()
                .HasKey(sc => new { sc.serviceid, sc.categoryid });

            modelBuilder.Entity<Servicecategories>()
                .HasOne(sc => sc.service)
                .WithMany(s => s.servicecategories)
                .HasForeignKey(sc => sc.serviceid);

            modelBuilder.Entity<Servicecategories>()
                .HasOne(sc => sc.category)
                .WithMany(c => c.servicecategories)
                .HasForeignKey(sc => sc.categoryid);
        }
    }
}