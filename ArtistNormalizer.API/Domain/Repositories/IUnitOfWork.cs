using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Repositories
{
    public interface IUnitOfWork
    {
        Task CompleteAsync();
    }
}
