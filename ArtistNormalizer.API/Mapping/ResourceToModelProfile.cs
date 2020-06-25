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
            //CreateMap<SaveAliasResource, Alias>();

            CreateMap<SaveAliasResource, Alias>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Alias));
            //.ForMember(dest => dest.Artist, 
            //                    opt => opt.MapFrom(src => Mapper.Map<Alias,Artist>(src)));
        }
    }
}
