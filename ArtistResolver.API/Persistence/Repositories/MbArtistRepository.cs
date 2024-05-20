using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Repositories;
using ArtistResolver.API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistResolver.API.Persistence.Repositories
{
    public class MbArtistRepository : BaseRepository, IMbArtistRepository
    {
        public MbArtistRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<MbArtist>> ListAsync(int? id, string mbId, string name, string originalName)
        {
            IQueryable<MbArtist> filter = context.MbArtists;

            if (id is not null)
            {
                filter = filter.Where(a => a.Id == id);
            }

            if (mbId is not null)
            {
                filter = filter.Where(a => a.MbId == mbId);
            }

            if (name is not null)
            {
                filter = filter.Where(a => a.Name == name);
            }

            if (originalName is not null)
            {
                filter = filter.Where(a => a.OriginalName == originalName);
            }

            return await filter
                .ToListAsync();
        }

        public async Task AddAsync(MbArtist artist)
        {
            await context.MbArtists.AddAsync(artist);
        }

        public void Remove(MbArtist artist)
        {
            context.MbArtists.Remove(artist);
        }

        public void Update(MbArtist artist)
        {
            context.MbArtists.Update(artist);
        }
    }
}
