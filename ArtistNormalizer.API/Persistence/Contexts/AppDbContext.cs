using ArtistNormalizer.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ArtistNormalizer.API.Persistence.Contexts
{
    public class AppDbContext : DbContext
    {
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Alias> Aliases { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Artist>().ToTable("Artists");
            builder.Entity<Artist>().HasKey(p => p.Id);
            builder.Entity<Artist>().HasIndex(d => d.Name).IsUnique();
            builder.Entity<Artist>().HasMany(p => p.Aliases).WithOne(p => p.Artist).HasForeignKey(p => p.ArtistId);

            builder.Entity<Alias>().ToTable("Aliases");
            builder.Entity<Alias>().HasKey(p => p.Id);
            builder.Entity<Alias>().HasIndex(d => d.Name).IsUnique();
        }
    }
}
