using System.ComponentModel.DataAnnotations;

namespace ArtistResolver.API.Resources
{
    public class SaveAliasResource
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public int artistid { get; set; }

        [Required]
        public int franchiseid { get; set; }
    }
}
