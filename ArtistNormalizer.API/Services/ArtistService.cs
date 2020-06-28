using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Domain.Services.Communication;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
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
            return await artistRepository.ListAsync();
        }

        public async Task<Artist> FindByIdAsync(int id)
        {
            return await artistRepository.FindByIdAsync(id);
        }

        public async Task<Artist> FindByNameAsync(string name)
        {
            return await artistRepository.FindByNameAsync(name);
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
                return new ArtistResponse($"An error occurred when saving artist: {ex.Message}");
            }
        }

        public async Task<ArtistResponse> DeleteAsync(int id)
        {
            var existingArtist = await artistRepository.FindByIdAsync(id);

            if (existingArtist == null)
                return new ArtistResponse("Artist not found.");

            try
            {
                artistRepository.Remove(existingArtist);
                await unitOfWork.CompleteAsync();

                return new ArtistResponse(existingArtist);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new ArtistResponse($"An error occurred when deleting artist: {ex.Message}");
            }
        }
    }
}
