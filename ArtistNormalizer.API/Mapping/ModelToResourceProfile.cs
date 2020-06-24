using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Resources;
using AutoMapper;

namespace ArtistNormalizer.API.Mapping
{
    public class ModelToResourceProfile : Profile
    {
        public ModelToResourceProfile()
        {
            CreateMap<Artist, ArtistResource>();
        }
    }
}
