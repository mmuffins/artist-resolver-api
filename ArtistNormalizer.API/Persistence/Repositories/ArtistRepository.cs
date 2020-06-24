using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Persistence.Repositories
{
    public class ArtistRepository : BaseRepository, IArtistRepository
    {
        public ArtistRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Artist>> ListAsync()
        {
            return await context.Artists.ToListAsync();
        }

        public async Task AddAsync(Artist artist)
        {
            await context.Artists.AddAsync(artist);
        }

        public async Task<Artist> FindByIdAsync(int id)
        {
            return await context.Artists.FindAsync(id);
        }

        public void Remove(Artist artist)
        {
            context.Artists.Remove(artist);
        }
    }
}
