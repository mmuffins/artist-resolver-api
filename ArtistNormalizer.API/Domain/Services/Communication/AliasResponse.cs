using ArtistNormalizer.API.Domain.Models;

namespace ArtistNormalizer.API.Domain.Services.Communication
{
    public class AliasResponse : BaseResponse
    {
        public Alias Alias { get; private set; }

        private AliasResponse(bool success, string message, Alias alias) : base(success, message)
        {
            Alias = alias;
        }

        /// <summary>
        /// Creates a success response.
        /// </summary>
        /// <param name="alias">Saved alias.</param>
        /// <returns>Response.</returns>
        public AliasResponse(Alias alias) : this(true, string.Empty, alias)
        { }

        /// <summary>
        /// Creates am error response.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <returns>Response.</returns>
        public AliasResponse(string message) : this(false, message, null)
        { }
    }
}
