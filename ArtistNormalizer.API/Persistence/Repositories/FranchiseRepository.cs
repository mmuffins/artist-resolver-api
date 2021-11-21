using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Persistence.Repositories
{
    public class FranchiseRepository : BaseRepository, IFranchiseRepository
    {
        public FranchiseRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Franchise>> ListAsync()
        {
            return await context.Franchises
                .Include(a => a.Aliases)
                .ToListAsync();
        }

        public async Task AddAsync(Franchise franchise)
        {
            await context.Franchises.AddAsync(franchise);
        }

        public async Task<Franchise> FindByIdAsync(int id)
        {
            return await context.Franchises
                .Include(a => a.Aliases)
                .SingleOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Franchise> FindByNameAsync(string name)
        {
            return await context.Franchises
                .Include(a => a.Aliases)
                .SingleOrDefaultAsync(a => string.Equals(a.Name, name, System.StringComparison.CurrentCultureIgnoreCase));
        }

        public void Remove(Franchise franchise)
        {
            context.Franchises.Remove(franchise);
        }
    }
}
