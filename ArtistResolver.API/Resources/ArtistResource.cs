using System.Collections.Generic;

namespace ArtistResolver.API.Resources
{
    public class ArtistResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<AliasResource> Aliases { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
