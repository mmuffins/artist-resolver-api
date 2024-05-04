using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Services
{
    public interface IMbArtistService
    {
        Task<IEnumerable<MbArtist>> ListAsync(int? id, string mbId);
        Task<MbArtistResponse> SaveAsync(MbArtist artist);
        Task<MbArtistResponse> UpdateAsync(MbArtist artist);
        Task<MbArtistResponse> DeleteAsync(int id);
    }
}
