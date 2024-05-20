using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistResolver.API.Domain.Services
{
    public interface IAliasService
    {
        Task<IEnumerable<Alias>> ListAsync(int? id, string name, int? franchiseId);
        Task<AliasResponse> SaveAsync(Alias alias);
        Task<AliasResponse> DeleteAsync(int id);
    }
}
