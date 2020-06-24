using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Extensions;
using ArtistNormalizer.API.Resources;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Controllers
{
    [Route("/api/[controller]")]
    public class ArtistController : Controller
    {
        private readonly IArtistService artistService;
        private readonly IMapper mapper;

        public ArtistController(IArtistService artistService, IMapper mapper)
        {
            this.artistService = artistService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<ArtistResource>> GetAllAsync()
        {
            var artists = await artistService.ListAsync();
            var resources = mapper.Map<IEnumerable<Artist>, IEnumerable<ArtistResource>>(artists);

            return resources;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveArtistResource resource)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            var artist = mapper.Map<SaveArtistResource, Artist>(resource);
            var result = await artistService.SaveAsync(artist);

            if (!result.Success)
                return BadRequest(result.Message);

            var artistResource = mapper.Map<Artist, ArtistResource>(result.Artist);
            return Ok(artistResource);
        }
    }
}
