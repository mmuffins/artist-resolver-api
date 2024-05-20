using ArtistResolver.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistResolver.API.Domain.Repositories
{
    public interface IAliasRepository
    {
        Task<IEnumerable<Alias>> ListAsync(int? id, string name, int? franchiseId);
        Task AddAsync(Alias alias);
        void Remove(Alias artist);
    }
}
