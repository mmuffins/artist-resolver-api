using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Resources;
using AutoMapper;

namespace ArtistNormalizer.API.Mapping
{
    public class ResourceToModelProfile : Profile
    {
        public ResourceToModelProfile()
        {
            CreateMap<SaveFranchiseResource, Franchise>();

            CreateMap<SaveArtistResource, Artist>();

            CreateMap<SaveAliasResource, Alias>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
        }
    }
}
