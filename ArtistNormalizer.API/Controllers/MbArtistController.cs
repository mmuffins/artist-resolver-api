using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Domain.Services;
using ArtistNormalizer.API.Domain.Services.Communication;
using ArtistNormalizer.API.Extensions;
using ArtistNormalizer.API.Resources;
using ArtistNormalizer.API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ArtistNormalizer.API.Controllers
{
    [Route("/api/[controller]")]
    public class MbArtistController : Controller
    {
        private readonly IMbArtistService mbArtistService;
        private readonly IMapper mapper;
        private readonly ILogger logger;

        public MbArtistController(IMbArtistService mbArtistService, IMapper mapper, ILogger<MbArtistController> logger)
        {
            this.mbArtistService = mbArtistService;
            this.mapper = mapper;
            this.logger = logger;
        }

        [HttpGet("id/{id}")]
        public async Task<MbArtistResource> FindByIdAsync(int id)
        {
            logger.LogInformation("GET /mbartist/id/" + id);
            var artist = (await mbArtistService.ListAsync(id, null)).FirstOrDefault();
            var resources = mapper.Map<MbArtist, MbArtistResource>(artist);
            return resources;
        }

        [HttpGet]
        public async Task<IEnumerable<MbArtistResource>> FindAsync(int? id, string mbId)
        {
            logger.LogInformation($"GET /mbartist - id:{id}, mbId:{mbId}");

            if (mbId is not null)
            {
                mbId = mbId.Trim();
            }

            IEnumerable<MbArtist> artist = await mbArtistService.ListAsync(id, mbId);
            var resource = mapper.Map<IEnumerable<MbArtist>, IEnumerable<MbArtistResource>>(artist);
            return resource;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] SaveMbArtistResource resource)
        {
            logger.LogInformation("POST /mbartist/ (Artist:" + resource.Name + ")");
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            resource.MbId = resource.MbId.Trim();
            resource.Name = resource.Name.Trim();
            resource.OriginalName = resource.OriginalName.Trim();

            MbArtist resolvedArtist = (await mbArtistService.ListAsync(null, resource.MbId)).FirstOrDefault();
            if (resolvedArtist != null)
            {
                return BadRequest("Artist with the specified MBID already exists.");
            }

            MbArtist artist = mapper.Map<SaveMbArtistResource, MbArtist>(resource);
            MbArtistResponse result = await mbArtistService.SaveAsync(artist);

            if (!result.Success)
                return BadRequest(result.Message);

            var artistResource = mapper.Map<MbArtist, MbArtistResource>(result.Artist);
            return Ok(artistResource);
        }

        [HttpPut("id/{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] SaveMbArtistResource resource)
        {
            logger.LogInformation($"PUT /mbartist/id/{id}");

            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());

            var existingArtist = (await mbArtistService.ListAsync(id, null)).FirstOrDefault();
            if (existingArtist == null)
                return NotFound();

            // Mapping updated data
            mapper.Map(resource, existingArtist);
            MbArtistResponse result = await mbArtistService.UpdateAsync(existingArtist);

            if (!result.Success)
                return BadRequest(result.Message);

            var updatedResource = mapper.Map<MbArtist, MbArtistResource>(result.Artist);
            return Ok(updatedResource);
        }
        [HttpDelete("id/{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            logger.LogInformation("DELETE /artist/id/" + id);
            var result = await mbArtistService.DeleteAsync(id);

            if (!result.Success)
                return BadRequest(result.Message);

            var categoryResource = mapper.Map<MbArtist, MbArtistResource>(result.Artist);
            return Ok(categoryResource);
        }
    }
}
