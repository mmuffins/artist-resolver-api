using ArtistNormalizer.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Repositories
{
    public interface IAliasRepository
    {
        Task<IEnumerable<Alias>> ListAsync(int? id, string name, int? franchiseId);
        Task AddAsync(Alias alias);
        void Remove(Alias artist);
    }
}
