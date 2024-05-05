using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Persistence.Repositories
{
    public class ArtistRepository : BaseRepository, IArtistRepository
    {
        public ArtistRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Artist>> ListAsync(int? id, string name)
        {
            IQueryable<Artist> filter = context.Artists;

            if (id is not null)
            {
                filter = filter.Where(a => a.Id == id);
            }

            if (name is not null)
            {
                filter = filter.Where(a => a.Name == name);
            }

            return await filter
                .Include(a => a.Aliases)
                .ToListAsync();
        }

        public async Task AddAsync(Artist artist)
        {
            await context.Artists.AddAsync(artist);
        }

        public void Remove(Artist artist)
        {
            context.Artists.Remove(artist);
        }
        
        public void Update(Artist artist)
        {
            context.Artists.Update(artist);
        }
    }
}
