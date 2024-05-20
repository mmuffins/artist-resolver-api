using System.Threading.Tasks;

namespace ArtistResolver.API.Domain.Repositories
{
    public interface IUnitOfWork
    {
        Task CompleteAsync();
    }
}
