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
    public class FranchiseService : IFranchiseService
    {
        private readonly IFranchiseRepository franchiseRepository;
        private readonly IUnitOfWork unitOfWork;

        public FranchiseService(IFranchiseRepository franchiseRepository, IUnitOfWork unitOfWork)
        {
            this.franchiseRepository = franchiseRepository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Franchise>> ListAsync()
        {
            return await franchiseRepository.ListAsync();
        }

        public async Task<Franchise> FindByIdAsync(int id)
        {
            return await franchiseRepository.FindByIdAsync(id);
        }

        public async Task<Franchise> FindByNameAsync(string name)
        {
            return await franchiseRepository.FindByNameAsync(name);
        }

        public async Task<FranchiseResponse> SaveAsync(Franchise franchise)
        {
            try
            {
                await franchiseRepository.AddAsync(franchise);
                await unitOfWork.CompleteAsync();

                return new FranchiseResponse(franchise);
            }
            catch (Exception ex)
            {
                return new FranchiseResponse($"An error occurred when saving franchise: {ex.Message}");
            }
        }

        public async Task<FranchiseResponse> DeleteAsync(int id)
        {
            var existingFranchise = await franchiseRepository.FindByIdAsync(id);

            if (existingFranchise == null)
                return new FranchiseResponse("Franchise not found.");

            try
            {
                franchiseRepository.Remove(existingFranchise);
                await unitOfWork.CompleteAsync();

                return new FranchiseResponse(existingFranchise);
            }
            catch (Exception ex)
            {
                // Do some logging stuff
                return new FranchiseResponse($"An error occurred when deleting franchise: {ex.Message}");
            }
        }
    }
}
