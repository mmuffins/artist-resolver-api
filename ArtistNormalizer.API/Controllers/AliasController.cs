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
    public class AliasController : Controller
    {
        private readonly IAliasService aliasService;
        private readonly IMapper mapper;
        private readonly IArtistService artistService;
        private readonly ILogger logger;

        public AliasController(IAliasService aliasService, IArtistService artistService, IMapper mapper, ILogger<ArtistController> logger)
        {
            this.aliasService = aliasService;
            this.artistService = artistService;
            this.mapper = mapper;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<AliasResource>> GetAllAsync()
        {
            logger.LogInformation("GET /alias");
            var aliases = await aliasService.ListAsync();
            var resources = mapper.Map<IEnumerable<Alias>, IEnumerable<AliasResource>>(aliases);

            return resources;
        }

        [HttpGet("name/{name}")]
        public async Task<AliasResource> FindByNameAsync(string name)
        {
            logger.LogInformation("GET /alias/name"+name);
            var alias = await aliasService.FindByNameAsync(name);
            var resource = mapper.Map<Alias, AliasResource>(alias);
            return resource;
        }

        [HttpGet("id/{id}")]
        public async Task<AliasResource> FindByIdAsync(int id)
        {
            logger.LogInformation("GET /alias/id/" + id);
            var alias = await aliasService.FindByIdAsync(id);
            var resource = mapper.Map<Alias, AliasResource>(alias);
            return resource;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveAliasResource resource)
        {
            logger.LogInformation("POST /alias/(Alias:" + resource.Name + ", Artist:" + resource.artistid + ")");
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            var resolvedAlias = await aliasService.FindByNameAsync(resource.Name);
            if (resolvedAlias != null)
            {
                if (resolvedAlias.ArtistId != resource.artistid)
                {
                    return BadRequest($"Alias already exists for artist {resolvedAlias.Artist.Id}");
                }

                var existingAlias = mapper.Map<Alias, AliasResource>(resolvedAlias);
                return Ok(existingAlias);
            }

            var resolvedArtist = await artistService.FindByIdAsync(resource.artistid);
            if (null == resolvedArtist)
            {
                return BadRequest($"Could not find artist with id {resource.artistid}");
            }

            var newAlias = new Alias()
            {
                ArtistId = resource.artistid,
                Name = resource.Name
            };

            var aliasResult = await aliasService.SaveAsync(newAlias);
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

            // Delete artist if we just deleted the last alias
            var artist = await artistService.FindByIdAsync(result.Alias.ArtistId);
            if(artist.Aliases.Count == 0)
            {
                var artistResult = await artistService.DeleteAsync(artist.Id);
                if (!artistResult.Success)
                    return BadRequest(artistResult.Message);
            }

            var categoryResource = mapper.Map<Alias, AliasResource>(result.Alias);
            return Ok(categoryResource);
        }

    }
}
