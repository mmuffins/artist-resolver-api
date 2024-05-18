using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistResolver.API.Domain.Services
{
    public interface IMbArtistService
    {
        Task<IEnumerable<MbArtist>> ListAsync(int? id, string mbId);
        Task<MbArtistResponse> SaveAsync(MbArtist artist);
        Task<MbArtistResponse> UpdateAsync(MbArtist artist);
        Task<MbArtistResponse> DeleteAsync(int id);
    }
}
