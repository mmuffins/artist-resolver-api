using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Resources;
using AutoMapper;

namespace ArtistNormalizer.API.Mapping
{
    public class ModelToResourceProfile : Profile
    {
        public ModelToResourceProfile()
        {
            CreateMap<Franchise, FranchiseResource>();

            CreateMap<Artist, ArtistResource>();

            CreateMap<MbArtist, MbArtistResource>();

            CreateMap<Alias, AliasResource>()
                .ForMember(dest => dest.Artist, opt => opt.MapFrom(src => src.Artist.Name));
        }
    }
}
