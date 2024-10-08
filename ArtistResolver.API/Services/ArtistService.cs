﻿using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Repositories;
using ArtistResolver.API.Domain.Services;
using ArtistResolver.API.Domain.Services.Communication;
using ArtistResolver.API.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistResolver.API.Services
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

        public async Task<IEnumerable<Artist>> ListAsync(int? id, string name)
        {
            return await artistRepository.ListAsync(id, name);
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
            var existingArtist = (await artistRepository.ListAsync(id, null))
                .FirstOrDefault();

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
        public async Task<ArtistResponse> UpdateAsync(Artist artist)
        {
            try
            {
                artistRepository.Update(artist);
                await unitOfWork.CompleteAsync();

                return new ArtistResponse(artist);
            }
            catch (Exception ex)
            {
                return new ArtistResponse($"An error occurred when updating the artist: {ex.Message}");
            }
        }


    }
}
