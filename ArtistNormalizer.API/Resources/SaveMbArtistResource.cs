using System.ComponentModel.DataAnnotations;

namespace ArtistNormalizer.API.Resources
{
    public class SaveMbArtistResource
    {
        [Required]
        public string MbId { get; set; }

        [Required]
        public string Name { get; set; }

        public string OriginalName { get; set; }

        [Required]
        public bool Include { get; set; }
    }
}
