using System.ComponentModel.DataAnnotations;

namespace ArtistNormalizer.API.Resources
{
    public class SaveAliasResource
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public int artistid { get; set; }
    }
}
