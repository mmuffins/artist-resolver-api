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

        public async Task<ArtistResponse> SaveAsync(Artist artist)
        {
            try
            {
                await artistRepository.AddAsync(artist);
                await unitOfWork.CompleteAsync();

                return new ArtistResponse(artist);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new ArtistResponse($"An error occurred when saving the artist: {ex.Message}");
            }
        }

        public async Task<ArtistResponse> DeleteAsync(int id)
        {
            var existingCategory = await artistRepository.FindByIdAsync(id);

            if (existingCategory == null)
                return new ArtistResponse("Category not found.");

            try
            {
                artistRepository.Remove(existingCategory);
                await unitOfWork.CompleteAsync();

                return new ArtistResponse(existingCategory);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new ArtistResponse($"An error occurred when deleting the category: {ex.Message}");
            }
        }
    }
}
