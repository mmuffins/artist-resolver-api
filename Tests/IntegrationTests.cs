using ArtistNormalizer.API;
using ArtistNormalizer.API.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    [Collection("Sequential")]
    public class IntegrationTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient client;

        public IntegrationTests(CustomWebApplicationFactory<Startup> factory)
        {
            client = factory.CreateClient();
        }

        [Fact]
        public async Task Artist_Post()
        {
            var endpoint = "/api/artist";

            var initialResponse = await client.GetAsync(endpoint);
            initialResponse.EnsureSuccessStatusCode();

            var initialCount = JsonSerializer.Deserialize<IEnumerable<Artist>>(await initialResponse.Content.ReadAsStringAsync());
            Assert.Empty(initialCount);

            // Add test data
            var artList = new List<Artist>();

            artList.Add(new Artist()
            {
                Name = "New Artist 1"
            });

            artList.Add(new Artist()
            {
                Name = "New Artist 2"
            });


            var JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            foreach (var art in artList)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = art.Name }), Encoding.UTF8, "application/json");
                var postResponse = await client.PostAsync(endpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<Artist>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(art.Name, postResponseObj.Name);
            }

            // Verify
            var httpResponse = await client.GetAsync(endpoint);
            httpResponse.EnsureSuccessStatusCode();

            // Deserialize and examine results.
            var verifyList = JsonSerializer.Deserialize<IEnumerable<Artist>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Collection(verifyList, item => Assert.Contains(artList[0].Name, item.Name),
                                            item => Assert.Contains(artList[1].Name, item.Name));
        }

        [Fact]
        public async Task Artist_Remove()
        {
            var endpoint = "/api/artist";
            var initialResponse = await client.GetAsync(endpoint);
            initialResponse.EnsureSuccessStatusCode();

            var JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var initialArtist = JsonSerializer.Deserialize<IEnumerable<Artist>>(await initialResponse.Content.ReadAsStringAsync(), JsonOptions).ToList();
            Assert.Equal(2, initialArtist.Count());

            var deleteArtist = initialArtist[0];
            var keepArtist = initialArtist[1];

            var deleteResponse = await client.DeleteAsync($"{endpoint}/{deleteArtist.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            var deleteResponseObj = JsonSerializer.Deserialize<Artist>(await deleteResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(deleteArtist.Id, deleteResponseObj.Id);

            // Verify
            var httpResponse = await client.GetAsync(endpoint);
            httpResponse.EnsureSuccessStatusCode();

            // Deserialize and examine results.
            var verifyList = JsonSerializer.Deserialize<IEnumerable<Artist>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions).ToList();

            Assert.Single(verifyList);
            Assert.Equal(keepArtist.Id, verifyList.First().Id);
        }

    }
}

