using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Repositories;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Domain.Services.Communication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Services
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

        public async Task<IEnumerable<Alias>> ListAsync()
        {
            return await aliasRepository.ListAsync();
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
            var existingAlias = await aliasRepository.FindByIdAsync(id);

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

        public async Task<Alias> FindByIdAsync(int id)
        {
            return await aliasRepository.FindByIdAsync(id);
        }

        public async Task<Alias> FindByNameAsync(string name)
        {
            return await aliasRepository.FindByNameAsync(name);
        }
    }
}
