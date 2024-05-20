
namespace ArtistResolver.API.Resources
{
    public class AliasResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ArtistId { get; set; }
        public string Artist { get; set; }
        public int FranchiseId { get; set; }
        public string Franchise { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
