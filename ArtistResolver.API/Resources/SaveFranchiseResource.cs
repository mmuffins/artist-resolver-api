using System.ComponentModel.DataAnnotations;

namespace ArtistResolver.API.Resources
{
    public class SaveFranchiseResource
    {
        [Required]
        public string Name { get; set; }
    }
}
