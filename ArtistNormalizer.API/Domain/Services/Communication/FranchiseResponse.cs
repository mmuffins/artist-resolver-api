using ArtistNormalizer.API.Domain.Models;

namespace ArtistNormalizer.API.Domain.Services.Communication
{
    public class FranchiseResponse : BaseResponse
    {
        public Franchise Franchise { get; private set; }

        private FranchiseResponse(bool success, string message, Franchise franchise) : base(success, message)
        {
            Franchise = franchise;
        }

        /// <summary>
        /// Creates a success response.
        /// </summary>
        /// <param name="franchise">Saved franchise.</param>
        /// <returns>Response.</returns>
        public FranchiseResponse(Franchise franchise) : this(true, string.Empty, franchise)
        { }

        /// <summary>
        /// Creates am error response.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <returns>Response.</returns>
        public FranchiseResponse(string message) : this(false, message, null)
        { }
    }
}
