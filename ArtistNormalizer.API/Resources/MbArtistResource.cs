﻿using System.Collections.Generic;

namespace ArtistNormalizer.API.Resources
{
    public class MbArtistResource
    {
        public int Id { get; set; }
        public string MbId { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public bool Include { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
