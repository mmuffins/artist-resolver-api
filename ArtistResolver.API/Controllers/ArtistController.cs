using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Domain.Services;
using ArtistResolver.API.Domain.Services.Communication;
using ArtistResolver.API.Extensions;
using ArtistResolver.API.Resources;
using ArtistResolver.API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistResolver.API.Controllers
{
    [Route("/api/[controller]")]
    public class ArtistController : Controller
    {
        private readonly IArtistService artistService;
        private readonly IMapper mapper;
        private readonly ILogger logger;

        public ArtistController(IArtistService artistService, IMapper mapper, ILogger<ArtistController> logger)
        {
            this.artistService = artistService;
            this.mapper = mapper;
            this.logger = logger;
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> FindByIdAsync(int id)
        {
            logger.LogInformation("GET /artist/id/" + id);
            var artist = (await artistService.ListAsync(id, null)).FirstOrDefault();
            if (artist == null)
            {
                return NotFound();
            }

            var resource = mapper.Map<Artist, ArtistResource>(artist);
            return Ok(resource);
        }

        [HttpGet]
        public async Task<IEnumerable<ArtistResource>> FindAsync(int? id, string name)
        {
            logger.LogInformation($"GET /artist - id:{id}, name:{name}");

            if (name is not null)
            {
                name = name.Trim();
            }

            IEnumerable<Artist> artist = await artistService.ListAsync(id, name);
            var resource = mapper.Map<IEnumerable<Artist>, IEnumerable<ArtistResource>>(artist);
            return resource;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveArtistResource resource)
        {
            logger.LogInformation("POST /artist/ (Artist:" + resource.Name + ")");
            
            resource.Name = resource.Name.Trim();
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            Artist resolvedArtist = (await artistService.ListAsync(null, resource.Name)).FirstOrDefault();
            if (resolvedArtist != null)
            {
                return Conflict("Artist with the specified name already exists.");
            }


            Artist artist = mapper.Map<SaveArtistResource, Artist>(resource);
            ArtistResponse result = await artistService.SaveAsync(artist);

            if (!result.Success)
                return BadRequest(result.Message);

            var artistResource = mapper.Map<Artist, ArtistResource>(result.Artist);
            return Ok(artistResource);
        }

        [HttpPut("id/{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] SaveArtistResource resource)
        {
            logger.LogInformation($"PUT /artist/id/{id}");

            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            var existingArtist = (await artistService.ListAsync(id, null)).FirstOrDefault();
            if (existingArtist == null)
                return NotFound();

            mapper.Map(resource, existingArtist);
            ArtistResponse result = await artistService.UpdateAsync(existingArtist);

            if (!result.Success)
                return BadRequest(result.Message);

            var updatedResource = mapper.Map<Artist, ArtistResource>(result.Artist);
            return Ok(updatedResource);
        }


        [HttpDelete("id/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            logger.LogInformation("DELETE /artist/id/" + id);
            var result = await artistService.DeleteAsync(id);

            if (!result.Success)
                return BadRequest(result.Message);

            var categoryResource = mapper.Map<Artist, ArtistResource>(result.Artist);
            return Ok(categoryResource);
        }
    }
}
