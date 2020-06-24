﻿using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services.Communication;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Domain.Services
{
    public interface IAliasService
    {
        Task<IEnumerable<Alias>> ListAsync();
        Task<AliasResponse> SaveAsync(Alias alias);
        Task<AliasResponse> DeleteAsync(int id);
    }
}
