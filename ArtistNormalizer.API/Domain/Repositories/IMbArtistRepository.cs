using ArtistNormalizer.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Repositories
{
    public interface IMbArtistRepository
    {
        Task<IEnumerable<MbArtist>> ListAsync(int? id, string mbId, string name, string originalName);
        Task AddAsync(MbArtist artist);
        void Remove(MbArtist artist);
        void Update(MbArtist artist);
    }
}
