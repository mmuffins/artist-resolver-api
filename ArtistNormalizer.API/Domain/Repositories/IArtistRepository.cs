using ArtistNormalizer.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Repositories
{
    public interface IArtistRepository
    {
        Task<IEnumerable<Artist>> ListAsync();
        Task AddAsync(Artist artist);
    }
}
