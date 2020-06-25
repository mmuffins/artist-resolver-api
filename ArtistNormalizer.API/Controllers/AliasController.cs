using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Extensions;
using ArtistNormalizer.API.Resources;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Controllers
{
    [Route("/api/[controller]")]
    public class AliasController : Controller
    {
        private readonly IAliasService aliasService;
        private readonly IMapper mapper;
        private readonly IArtistService artistService;

        public AliasController(IAliasService aliasService, IArtistService artistService, IMapper mapper)
        {
            this.aliasService = aliasService;
            this.artistService = artistService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<AliasResource>> GetAllAsync()
        {
            var aliases = await aliasService.ListAsync();
            var resources = mapper.Map<IEnumerable<Alias>, IEnumerable<AliasResource>>(aliases);

            return resources;
        }

        [HttpGet("name/{name}")]
        public async Task<AliasResource> FindByNameAsync(string name)
        {
            var alias = await aliasService.FindByNameAsync(name);
            var resource = mapper.Map<Alias, AliasResource>(alias);
            return resource;
        }

        [HttpGet("id/{id}")]
        public async Task<AliasResource> FindByIdAsync(int id)
        {
            var alias = await aliasService.FindByIdAsync(id);
            var resource = mapper.Map<Alias, AliasResource>(alias);
            return resource;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveAliasResource resource)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            var resolvedAlias = await aliasService.FindByNameAsync(resource.Alias);
            var resolvedArtist = await artistService.FindByNameAsync(resource.Artist);

            if (null == resolvedAlias)
            {
                if(null == resolvedArtist)
                {
                    // artist is new, create it
                    var newArtist = new Artist()
                    {
                        Name = resource.Artist
                    };
                    var artistResult = await artistService.SaveAsync(newArtist);
                    if (!artistResult.Success)
                        return BadRequest(artistResult.Message);

                    resolvedArtist = artistResult.Artist;
                }

                var newAlias = new Alias()
                {
                    ArtistId = resolvedArtist.Id,
                    Name = resource.Alias
                };

                var aliasResult = await aliasService.SaveAsync(newAlias);
                if (!aliasResult.Success)
                    return BadRequest(aliasResult.Message);

                resolvedAlias = aliasResult.Alias;
            }

            var aliasResource = mapper.Map<Alias, AliasResource>(resolvedAlias);
            return Ok(aliasResource);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
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
