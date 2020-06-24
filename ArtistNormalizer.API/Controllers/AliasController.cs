using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Extensions;
using ArtistNormalizer.API.Resources;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Controllers
{
    [Route("/api/[controller]")]
    public class AliasController : Controller
    {
        private readonly IAliasService aliasService;
        private readonly IMapper mapper;

        public AliasController(IAliasService aliasService, IMapper mapper)
        {
            this.aliasService = aliasService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<AliasResource>> GetAllAsync()
        {
            var aliases = await aliasService.ListAsync();
            var resources = mapper.Map<IEnumerable<Alias>, IEnumerable<AliasResource>>(aliases);

            return resources;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveAliasResource resource)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            var alias = mapper.Map<SaveAliasResource, Alias>(resource);
            var result = await aliasService.SaveAsync(alias);

            if (!result.Success)
                return BadRequest(result.Message);

            var aliasResource = mapper.Map<Alias, AliasResource>(result.Alias);
            return Ok(aliasResource);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

    }
}
