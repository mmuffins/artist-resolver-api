using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Resources;
using AutoMapper;

namespace ArtistNormalizer.API.Mapping
{
    public class ResourceToModelProfile : Profile
    {
        public ResourceToModelProfile()
        {
            CreateMap<SaveArtistResource, Artist>();
        }
    }
}
