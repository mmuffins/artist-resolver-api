using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Extensions;
using ArtistNormalizer.API.Resources;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Controllers
{
    [Route("/api/[controller]")]
    public class FranchiseController : Controller
    {
        private readonly IFranchiseService franchiseService;
        private readonly IMapper mapper;
        private readonly ILogger logger;

        public FranchiseController(IFranchiseService franchiseService, IMapper mapper, ILogger<FranchiseController> logger)
        {
            this.franchiseService = franchiseService;
            this.mapper = mapper;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<FranchiseResource>> GetAllAsync()
        {
            logger.LogInformation("GET /franchise/");
            var franchises = await franchiseService.ListAsync();
            var resources = mapper.Map<IEnumerable<Franchise>, IEnumerable<FranchiseResource>>(franchises);
            return resources;
        }

        [HttpGet("id/{id}")]
        public async Task<FranchiseResource> FindByIdAsync(int id)
        {
            logger.LogInformation("GET /franchise/id/" + id);
            var franchise = await franchiseService.FindByIdAsync(id);
            var resources = mapper.Map<Franchise, FranchiseResource>(franchise);
            return resources;
        }

        [HttpGet("name/{name}")]
        public async Task<FranchiseResource> FindByNameAsync(string name)
        {
            logger.LogInformation("GET /franchise/name/" + name);
            
            name = name.Trim();
            var franchise = await franchiseService.FindByNameAsync(name);
            var resources = mapper.Map<Franchise, FranchiseResource>(franchise);
            return resources;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveFranchiseResource resource)
        {
            logger.LogInformation("POST /franchise/ (Artist:" + resource.Name + ")");
            
            resource.Name = resource.Name.Trim();
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            var resolvedFranchise = await franchiseService.FindByNameAsync(resource.Name);
            if (resolvedFranchise != null)
            {
                var existingAlias = mapper.Map<Franchise, FranchiseResource>(resolvedFranchise);
                return Ok(existingAlias);
            }

            var franchise = mapper.Map<SaveFranchiseResource, Franchise>(resource);
            var result = await franchiseService.SaveAsync(franchise);

            if (!result.Success)
                return BadRequest(result.Message);

            var FranchiseResource = mapper.Map<Franchise, FranchiseResource>(result.Franchise);
            return Ok(FranchiseResource);
        }

        [HttpDelete("id/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            logger.LogInformation("DELETE /franchise/id/" + id);
            var result = await franchiseService.DeleteAsync(id);

            if (!result.Success)
                return BadRequest(result.Message);

            var categoryResource = mapper.Map<Franchise, FranchiseResource>(result.Franchise);
            return Ok(categoryResource);
        }
    }
}
