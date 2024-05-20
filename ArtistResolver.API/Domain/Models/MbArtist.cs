using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtistResolver.API.Domain.Models
{
    public class MbArtist
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string MbId { get; set; }
        [Required]
        public string Name { get; set; }
        public string OriginalName { get; set; }
        [Required]
        public bool Include { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
