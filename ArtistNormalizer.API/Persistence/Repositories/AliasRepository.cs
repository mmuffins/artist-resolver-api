
using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Persistence.Repositories
{
    public class AliasRepository : BaseRepository, IAliasRepository
    {
        public AliasRepository(AppDbContext context) : base(context) { }

        public async Task AddAsync(Alias alias)
        {
            await context.Aliases.AddAsync(alias);
        }

        public async Task<IEnumerable<Alias>> ListAsync(int? id, string name, int? franchiseId)
        {
            IQueryable<Alias> filter = context.Aliases;
            
            if (id is not null)
            {
                filter = filter.Where(a => a.Id == id);
            }

            if (name is not null)
            {
                filter = filter.Where(a => a.Name == name);
            }

            if (franchiseId is not null)
            {
                filter = filter.Where(a => a.FranchiseId == franchiseId);
            }

            return await filter
                .Include(a => a.Franchise)
                .Include(a => a.Artist)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alias>> ListAsync()
        {
            return await context.Aliases
                .Include(a => a.Franchise)
                .Include(a => a.Artist)
                .ToListAsync();
        }

        public void Remove(Alias artist)
        {
            context.Aliases.Remove(artist);
        }
    }
}
