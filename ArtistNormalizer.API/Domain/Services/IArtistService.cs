using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Services
{
    public interface IArtistService
    {
        Task<IEnumerable<Artist>> ListAsync();
        Task<Artist> FindByIdAsync(int id);
        Task<Artist> FindByNameAsync(string name);
        Task<ArtistResponse> SaveAsync(Artist artist);
        Task<ArtistResponse> DeleteAsync(int id);
    }
}
