using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Domain.Services.Communication;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Services
{
    public class MbArtistService : IMbArtistService
    {
        private readonly IMbArtistRepository mbArtistRepository;
        private readonly IUnitOfWork unitOfWork;

        public MbArtistService(IMbArtistRepository artistRepository, IUnitOfWork unitOfWork)
        {
            this.mbArtistRepository = artistRepository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<MbArtist>> ListAsync(int? id, string mbId)
        {
            return await mbArtistRepository.ListAsync(id, mbId, null, null);
        }

        public async Task<MbArtistResponse> SaveAsync(MbArtist artist)
        {
            try
            {
                await mbArtistRepository.AddAsync(artist);
                await unitOfWork.CompleteAsync();

                return new MbArtistResponse(artist);
            }
            catch (Exception ex)
            {
                return new MbArtistResponse($"An error occurred when saving artist: {ex.Message}");
            }
        }

        public async Task<MbArtistResponse> DeleteAsync(int id)
        {
            var existingArtist = (await mbArtistRepository.ListAsync(id, null, null, null))
                .FirstOrDefault();

            if (existingArtist == null)
                return new MbArtistResponse("Artist not found.");

            try
            {
                mbArtistRepository.Remove(existingArtist);
                await unitOfWork.CompleteAsync();

                return new MbArtistResponse(existingArtist);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new MbArtistResponse($"An error occurred when deleting artist: {ex.Message}");
            }
        }

        public async Task<MbArtistResponse> UpdateAsync(MbArtist artist)
        {
            try
            {
                mbArtistRepository.Update(artist);
                await unitOfWork.CompleteAsync();

                return new MbArtistResponse(artist);
            }
            catch (Exception ex)
            {
                return new MbArtistResponse($"An error occurred when updating the artist: {ex.Message}");
            }
        }
    }
}
