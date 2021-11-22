using ArtistNormalizer.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Repositories
{
    public interface IFranchiseRepository
    {
        Task<IEnumerable<Franchise>> ListAsync(int? id, string name);
        Task AddAsync(Franchise franchise);
        void Remove(Franchise franchise);
    }
}
