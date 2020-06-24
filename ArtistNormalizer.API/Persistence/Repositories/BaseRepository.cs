using ArtistNormalizer.API.Persistence.Contexts;

namespace ArtistNormalizer.API.Persistence.Repositories
{
    public abstract class BaseRepository
    {
        protected readonly AppDbContext context;

        public BaseRepository(AppDbContext context)
        {
            this.context = context;
        }
    }
}
