using System.ComponentModel.DataAnnotations;

namespace ArtistResolver.API.Resources
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

        [Required]
        public string Type { get; set; }
    }
}
