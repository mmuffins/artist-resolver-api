using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Domain.Services.Communication;
using ArtistNormalizer.API.Extensions;
using ArtistNormalizer.API.Resources;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
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

        [HttpGet("id/{id}")]
        public async Task<FranchiseResource> FindByIdAsync(int id)
        {
            logger.LogInformation("GET /franchise/id/" + id);
            var franchise = (await franchiseService.ListAsync(id, null)).FirstOrDefault();
            var resources = mapper.Map<Franchise, FranchiseResource>(franchise);
            return resources;
        }

        [HttpGet]
        public async Task<IEnumerable<FranchiseResource>> FindAsync(int? id, string name)
        {
            logger.LogInformation($"GET /franchise - id:{id}, name:{name}");

            if (name is not null)
            {
                name = name.Trim();
            }

            IEnumerable<Franchise> franchise = await franchiseService.ListAsync(id, name);
            var resource = mapper.Map<IEnumerable<Franchise>, IEnumerable<FranchiseResource>>(franchise);
            return resource;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveFranchiseResource resource)
        {
            logger.LogInformation("POST /franchise/ (Artist:" + resource.Name + ")");
            
            resource.Name = resource.Name.Trim();
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            Franchise resolvedFranchise = (await franchiseService.ListAsync(null, resource.Name)).FirstOrDefault();
            if (resolvedFranchise != null)
            {
                var existingAlias = mapper.Map<Franchise, FranchiseResource>(resolvedFranchise);
                return Ok(existingAlias);
            }

            Franchise franchise = mapper.Map<SaveFranchiseResource, Franchise>(resource);
            FranchiseResponse result = await franchiseService.SaveAsync(franchise);

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
