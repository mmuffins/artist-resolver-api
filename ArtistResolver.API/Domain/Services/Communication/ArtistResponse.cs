using ArtistResolver.API.Domain.Models;

namespace ArtistResolver.API.Domain.Services.Communication
{
    public class ArtistResponse : BaseResponse
    {
        public Artist Artist { get; private set; }

        private ArtistResponse(bool success, string message, Artist artist) : base(success, message)
        {
            Artist = artist;
        }

        /// <summary>
        /// Creates a success response.
        /// </summary>
        /// <param name="artist">Saved artist.</param>
        /// <returns>Response.</returns>
        public ArtistResponse(Artist artist) : this(true, string.Empty, artist)
        { }

        /// <summary>
        /// Creates am error response.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <returns>Response.</returns>
        public ArtistResponse(string message) : this(false, message, null)
        { }
    }
}
