using System.ComponentModel.DataAnnotations;

namespace ArtistResolver.API.Resources
{
    public class SaveArtistResource
    {
        [Required]
        public string Name { get; set; }
    }
}
