using System.ComponentModel.DataAnnotations;

namespace ArtistNormalizer.API.Resources
{
    public class SaveArtistResource
    {
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
    }
}
