using ArtistNormalizer.API.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtistNormalizer.API.Resources
{
    public class AliasResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ArtistId { get; set; }
        public Artist Artist { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
