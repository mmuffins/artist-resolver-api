﻿using System.ComponentModel.DataAnnotations;

namespace ArtistNormalizer.API.Resources
{
    public class SaveArtistResource
    {
        [Required]
        public string Name { get; set; }
    }
}
