using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Resources;
using AutoMapper;

namespace ArtistResolver.API.Mapping
{
    public class ResourceToModelProfile : Profile
    {
        public ResourceToModelProfile()
        {
            CreateMap<SaveFranchiseResource, Franchise>();

            CreateMap<SaveArtistResource, Artist>();
            
            CreateMap<SaveMbArtistResource, MbArtist>();

            CreateMap<SaveAliasResource, Alias>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
        }
    }
}
