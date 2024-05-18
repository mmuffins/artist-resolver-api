using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtistResolver.API.Domain.Models
{
    public class Franchise
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public IList<Alias> Aliases { get; set; } = new List<Alias>();

        public override string ToString()
        {
            return Name;
        }
    }
}
