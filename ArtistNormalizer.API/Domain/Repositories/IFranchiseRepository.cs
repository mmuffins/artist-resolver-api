using ArtistNormalizer.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Repositories
{
    public interface IFranchiseRepository
    {
        Task<IEnumerable<Franchise>> ListAsync();
        Task AddAsync(Franchise artist);
        Task<Franchise> FindByIdAsync(int id);
        Task<Franchise> FindByNameAsync(string name);
        void Remove(Franchise franchise);
    }
}
