﻿using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Repositories;
using ArtistResolver.API.Domain.Services;
using ArtistResolver.API.Domain.Services.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistResolver.API.Services
{
    public class AliasService : IAliasService
    {
        private readonly IAliasRepository aliasRepository;
        private readonly IUnitOfWork unitOfWork;

        public AliasService(IAliasRepository aliasRepository, IUnitOfWork unitOfWork)
        {
            this.aliasRepository = aliasRepository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Alias>> ListAsync(int? id, string name, int? franchiseId)
        {
            return await aliasRepository.ListAsync(id, name, franchiseId);
        }

        public async Task<AliasResponse> SaveAsync(Alias alias)
        {
            try
            {
                await aliasRepository.AddAsync(alias);
                await unitOfWork.CompleteAsync();

                return new AliasResponse(alias);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new AliasResponse($"An error occurred when saving alias: {ex.Message}");
            }
        }

        public async Task<AliasResponse> DeleteAsync(int id)
        {
            var existingAlias = (await aliasRepository.ListAsync(id, null, null))
                .FirstOrDefault();

            if (existingAlias == null)
                return new AliasResponse("Alias not found.");

            try
            {
                aliasRepository.Remove(existingAlias);
                await unitOfWork.CompleteAsync();

                return new AliasResponse(existingAlias);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new AliasResponse($"An error occurred when deleting alias: {ex.Message}");
            }
        }
    }
}
