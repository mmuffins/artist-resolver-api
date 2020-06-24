using ArtistNormalizer.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ArtistNormalizer.API.Persistence.Contexts
{
    public class AppDbContext : DbContext
    {
        public DbSet<Artist> Artists { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Artist>().ToTable("Artists");
            builder.Entity<Artist>().HasKey(p => p.Id);
            builder.Entity<Artist>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();

            builder.Entity<Artist>().HasData
            (
                new Artist { Id = 100, Name = "Artist 1" }, // Id set manually due to in-memory provider
                new Artist { Id = 101, Name = "Artist 2" }
            );
        }
    }
}
