using ArtistResolver.API.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistResolver.API.Domain.Repositories
{
    public interface IMbArtistRepository
    {
        Task<IEnumerable<MbArtist>> ListAsync(int? id, string mbId, string name, string originalName);
        Task AddAsync(MbArtist artist);
        void Remove(MbArtist artist);
        void Update(MbArtist artist);
    }
}
