using System.ComponentModel.DataAnnotations;

namespace ArtistNormalizer.API.Resources
{
    public class SaveAliasResource
    {
        [Required]
        public string Alias { get; set; }

        [Required]
        public string Artist { get; set; }
    }
}
