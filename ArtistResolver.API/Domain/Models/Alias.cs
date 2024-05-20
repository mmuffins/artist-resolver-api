using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtistResolver.API.Domain.Models
{
    public class Alias
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int ArtistId { get; set; }
        public int FranchiseId { get; set; }
        public Artist Artist { get; set; }
        public Franchise Franchise { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
