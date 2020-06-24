using ArtistNormalizer.API;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class PlayersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;

        public PlayersControllerIntegrationTests(CustomWebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CanGetPlayers()
        {
            // The endpoint or route of the controller action.
            var httpResponse = await _client.GetAsync("/api/artist");

            // Must be successful.
            httpResponse.EnsureSuccessStatusCode();

            // Deserialize and examine results.
            var stringResponse = await httpResponse.Content.ReadAsStringAsync();
            //var players = JsonConvert.DeserializeObject<IEnumerable<Player>>(stringResponse);
            //Assert.Contains(players, p => p.FirstName == "Wayne");
            //Assert.Contains(players, p => p.FirstName == "Mario");
        }





    }
}
