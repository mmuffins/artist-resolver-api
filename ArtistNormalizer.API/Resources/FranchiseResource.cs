using System.Collections.Generic;

namespace ArtistNormalizer.API.Resources
{
    public class FranchiseResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
