using System.ComponentModel.DataAnnotations;

namespace ArtistNormalizer.API.Resources
{
    public class SaveFranchiseResource
    {
        [Required]
        public string Name { get; set; }
    }
}
