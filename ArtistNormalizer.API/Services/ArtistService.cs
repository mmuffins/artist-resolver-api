using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Domain.Services.Communication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Services
{
    public class ArtistService : IArtistService
    {
        private readonly IArtistRepository artistRepository;
        private readonly IUnitOfWork unitOfWork;

        public ArtistService(IArtistRepository artistRepository, IUnitOfWork unitOfWork)
        {
            this.artistRepository = artistRepository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Artist>> ListAsync()
        {
            return await this.artistRepository.ListAsync();
        }

        public async Task<SaveArtistResponse> SaveAsync(Artist artist)
        {
            try
            {
                await artistRepository.AddAsync(artist);
                await unitOfWork.CompleteAsync();

                return new SaveArtistResponse(artist);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new SaveArtistResponse($"An error occurred when saving the artist: {ex.Message}");
            }
        }
    }
}
