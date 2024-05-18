using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Repositories;
using ArtistResolver.API.Domain.Services;
using ArtistResolver.API.Domain.Services.Communication;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistResolver.API.Services
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

        public async Task<IEnumerable<Franchise>> ListAsync(int? id, string name)
        {
            return await franchiseRepository.ListAsync(id, name);
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
            var existingFranchise = (await franchiseRepository.ListAsync(id, null))
                .FirstOrDefault();

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
