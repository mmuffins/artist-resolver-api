namespace ArtistNormalizer.API.Resources
{
    public class ArtistResource
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
