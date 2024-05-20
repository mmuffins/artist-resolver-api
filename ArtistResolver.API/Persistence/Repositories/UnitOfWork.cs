using ArtistResolver.API.Domain.Repositories;
using ArtistResolver.API.Persistence.Contexts;
using System.Threading.Tasks;

namespace ArtistResolver.API.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext context;

        public UnitOfWork(AppDbContext context)
        {
            this.context = context;
        }

        public async Task CompleteAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
