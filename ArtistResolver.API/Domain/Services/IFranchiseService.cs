using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistResolver.API.Domain.Services
{
    public interface IFranchiseService
    {
        Task<IEnumerable<Franchise>> ListAsync(int? id, string name);
        Task<FranchiseResponse> SaveAsync(Franchise franchise);
        Task<FranchiseResponse> DeleteAsync(int id);
    }
}
