using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Services
{
    public interface IFranchiseService
    {
        Task<IEnumerable<Franchise>> ListAsync(int? id, string name);
        Task<FranchiseResponse> SaveAsync(Franchise franchise);
        Task<FranchiseResponse> DeleteAsync(int id);
    }
}
