using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Services
{
    public interface IArtistService
    {
        Task<IEnumerable<Artist>> ListAsync();
        Task<SaveArtistResponse> SaveAsync(Artist artist);
    }
}
