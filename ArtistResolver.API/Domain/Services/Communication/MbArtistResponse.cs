using ArtistResolver.API.Domain.Models;

namespace ArtistResolver.API.Domain.Services.Communication
{
    public class MbArtistResponse : BaseResponse
    {
        public MbArtist Artist { get; private set; }

        private MbArtistResponse(bool success, string message, MbArtist artist) : base(success, message)
        {
            Artist = artist;
        }

        /// <summary>
        /// Creates a success response.
        /// </summary>
        /// <param name="artist">Saved artist.</param>
        /// <returns>Response.</returns>
        public MbArtistResponse(MbArtist artist) : this(true, string.Empty, artist)
        { }

        /// <summary>
        /// Creates am error response.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <returns>Response.</returns>
        public MbArtistResponse(string message) : this(false, message, null)
        { }
    }
}
