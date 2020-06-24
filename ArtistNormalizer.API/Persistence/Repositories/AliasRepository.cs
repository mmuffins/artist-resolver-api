﻿
using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Persistence.Repositories
{
    public class AliasRepository : BaseRepository, IAliasRepository
    {
        public AliasRepository(AppDbContext context) : base(context) { }

        public async Task AddAsync(Alias alias)
        {
            await context.Aliases.AddAsync(alias);
        }

        public async Task<Alias> FindByIdAsync(int id)
        {
            return await context.Aliases.FindAsync(id);
        }

        public async Task<Alias> FindByNameAsync(string name)
        {
            return await context.Aliases
                .Where(a => a.Name == name)
                .FirstAsync();
        }

        public async Task<IEnumerable<Alias>> ListAsync()
        {
            return await context.Aliases.ToListAsync();
        }

        public void Remove(Alias artist)
        {
            context.Aliases.Remove(artist);
        }
    }
}
