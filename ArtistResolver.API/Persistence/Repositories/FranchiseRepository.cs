using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Repositories;
using ArtistResolver.API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistResolver.API.Persistence.Repositories
{
    public class FranchiseRepository : BaseRepository, IFranchiseRepository
    {
        public FranchiseRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Franchise>> ListAsync(int? id, string name)
        {
            IQueryable<Franchise> filter = context.Franchises;

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

        public async Task AddAsync(Franchise franchise)
        {
            await context.Franchises.AddAsync(franchise);
        }

        public void Remove(Franchise franchise)
        {
            context.Franchises.Remove(franchise);
        }
    }
}
