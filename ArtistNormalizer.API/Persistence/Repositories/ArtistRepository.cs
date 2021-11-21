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
            return await context.Artists
                .Include(a => a.Aliases)
                .ToListAsync();
        }

        public async Task AddAsync(Artist artist)
        {
            await context.Artists.AddAsync(artist);
        }

        public async Task<Artist> FindByIdAsync(int id)
        {
            return await context.Artists
                .Include(a => a.Aliases)
                .SingleOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Artist> FindByNameAsync(string name)
        {
            return await context.Artists
                .Include(a => a.Aliases)
                .SingleOrDefaultAsync(a => a.Name == name);
        }

        public void Remove(Artist artist)
        {
            context.Artists.Remove(artist);
        }
    }
}
