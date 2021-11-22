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
    public class AliasController : Controller
    {
        private readonly IAliasService aliasService;
        private readonly IArtistService artistService;
        private readonly IFranchiseService franchiseService;
        private readonly IMapper mapper;
        private readonly ILogger logger;

        public AliasController(IAliasService aliasService, IArtistService artistService, IFranchiseService franchiseService, IMapper mapper, ILogger<ArtistController> logger)
        {
            this.aliasService = aliasService;
            this.artistService = artistService;
            this.franchiseService = franchiseService;
            this.mapper = mapper;
            this.logger = logger;
        }

        [HttpGet("id/{id}")]
        public async Task<AliasResource> FindByIdAsync(int id)
        {
            logger.LogInformation("GET /alias/id/" + id);

            Alias alias = (await aliasService.ListAsync(id, null, null)).FirstOrDefault();
            var resource = mapper.Map<Alias, AliasResource>(alias);
            return resource;
        }

        [HttpGet]
        public async Task<IEnumerable<AliasResource>> FindAsync(int? id, string name, string franchise, int? franchiseId)
        {
            logger.LogInformation($"GET /alias - id:{id}, name:{name}, franchise:{franchise}, franchiseId:{franchiseId}");

            if (name is not null)
            {
                name = name.Trim();
            }
            if (franchise is not null)
            {
                franchise = franchise.Trim();
            }

            int? resolvedFranchiseId = null;
            if(franchise is not null)
            {
                // make sure to always assign a dummy value if the franchise parameter was passed
                // otherwise the query below would return null which is then passed to the aliasService.ListAsync
                // function that treats it like no franchise was provided at all
                resolvedFranchiseId = -1;
                
                Franchise resolvedFranchise = (await franchiseService.ListAsync(null, franchise)).FirstOrDefault();
                if(resolvedFranchise is not null)
                {
                    resolvedFranchiseId = resolvedFranchise.Id;
                }
            }

            IEnumerable<Alias> alias = await aliasService.ListAsync(id, name, resolvedFranchiseId);
            var resource = mapper.Map<IEnumerable<Alias>, IEnumerable<AliasResource>>(alias);
            return resource;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveAliasResource resource)
        {
            logger.LogInformation("POST /alias/ (Alias:" + resource.Name + ", Artist:" + resource.artistid + ", Franchise:" + resource.franchiseid + ")");
            
            resource.Name = resource.Name.Trim();
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            Alias resolvedAlias = (await aliasService.ListAsync(null, resource.Name, resource.franchiseid)).FirstOrDefault();
            if (resolvedAlias != null)
            {
                if (resolvedAlias.ArtistId != resource.artistid)
                {
                    return BadRequest($"Alias already exists for artist {resolvedAlias.Artist.Id} with franchise {resolvedAlias.Franchise.Id}");
                }

                var existingAlias = mapper.Map<Alias, AliasResource>(resolvedAlias);
                return Ok(existingAlias);
            }

            Artist resolvedArtist = (await artistService.ListAsync(resource.artistid, null)).FirstOrDefault();
            if (null == resolvedArtist)
            {
                return BadRequest($"Could not find artist with id {resource.artistid}");
            }

            Franchise resolvedFranchise = (await franchiseService.ListAsync(resource.franchiseid, null)).FirstOrDefault();
            if (null == resolvedFranchise)
            {
                return BadRequest($"Could not find franchise with id {resource.franchiseid}");
            }

            var newAlias = new Alias()
            {
                ArtistId = resource.artistid,
                FranchiseId = resource.franchiseid,
                Name = resource.Name
            };

            AliasResponse aliasResult = await aliasService.SaveAsync(newAlias);
            if (!aliasResult.Success)
                return BadRequest(aliasResult.Message);

            resolvedAlias = aliasResult.Alias;

            var aliasResource = mapper.Map<Alias, AliasResource>(resolvedAlias);
            return Ok(aliasResource);
        }

        [HttpDelete("id/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            logger.LogInformation("DELETE /alias/id/" + id);
            var result = await aliasService.DeleteAsync(id);
            if (!result.Success)
                return BadRequest(result.Message);

            // Delete artist if we just deleted its last alias
            Artist artist = (await artistService.ListAsync(result.Alias.ArtistId, null)).FirstOrDefault();
            if (artist.Aliases.Count == 0)
            {
                var artistResult = await artistService.DeleteAsync(artist.Id);
                if (!artistResult.Success)
                    return BadRequest(artistResult.Message);
            }

            // Delete franchise if we just deleted its last alias
            Franchise franchise = (await franchiseService.ListAsync(result.Alias.FranchiseId, null)).FirstOrDefault();
            if (franchise.Aliases.Count == 0)
            {
                var franchiseResult = await franchiseService.DeleteAsync(franchise.Id);
                if (!franchiseResult.Success)
                    return BadRequest(franchiseResult.Message);
            }

            var categoryResource = mapper.Map<Alias, AliasResource>(result.Alias);
            return Ok(categoryResource);
        }

    }
}
