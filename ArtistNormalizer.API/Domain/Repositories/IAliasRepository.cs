using ArtistNormalizer.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Repositories
{
    public interface IAliasRepository
    {
        Task<IEnumerable<Alias>> ListAsync();
        Task AddAsync(Alias alias);
        Task<Alias> FindByIdAsync(int id);
        Task<Alias> FindByNameAsync(string name);
        void Remove(Alias artist);
    }
}
