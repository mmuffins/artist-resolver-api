using ArtistNormalizer.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ArtistNormalizer.API.Persistence.Contexts
{
    public class AppDbContext : DbContext
    {
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Alias> Aliases { get; set; }
        public DbSet<Franchise> Franchises { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Artist>(e =>
            {
                e.ToTable("Artists");
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.Name).IsUnique();
                e.Property(x => x.Name).HasColumnType("TEXT COLLATE NOCASE");
                e.HasMany(p => p.Aliases).WithOne(p => p.Artist).HasForeignKey(p => p.ArtistId);
            });

            builder.Entity<Franchise>(e =>
            {
                e.ToTable("Franchises");
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.Name).IsUnique();
                //e.Property(p => p.Name).HasConversion(v => v.ToLowerInvariant(), v => v);
                e.Property(x => x.Name).HasColumnType("TEXT COLLATE NOCASE");
                e.HasMany(p => p.Aliases).WithOne(p => p.Franchise).HasForeignKey(p => p.FranchiseId);
            });

            builder.Entity<Alias>(e => {
                e.ToTable("Aliases");
                e.HasKey(p => p.Id);
                e.HasIndex(p => new { p.Name, p.FranchiseId }).IsUnique();
                e.Property(p => p.Name).HasConversion(v => v.ToLowerInvariant(), v => v);
                e.Property(x => x.Name).HasColumnType("TEXT COLLATE NOCASE");
            });
        }
    }
}
