using ArtistNormalizer.API;
using ArtistNormalizer.API.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Integrationtests
{

    [Collection("Sequential")]
    public class MbArtistTests : BaseIntegrationTests
    {
        public MbArtistTests(CustomWebApplicationFactory<Startup> factory) : base(factory) { }

        [Fact]
        public async Task MbArtist_FindById()
        {
            // Add test data
            await SeedData(0, 0, 0, 5);

            // Verify that an invalid id returns nothing
            HttpResponseMessage verifyNotFoundResponse = await client.GetAsync($"{mbArtistEndpoint}/id/999999");
            verifyNotFoundResponse.EnsureSuccessStatusCode();

            var verifyNotFoundString = await verifyNotFoundResponse.Content.ReadAsStringAsync();
            Assert.Empty(verifyNotFoundString);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allArtistsResponse = await client.GetAsync(mbArtistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtistsList = JsonSerializer.Deserialize<IEnumerable<MbArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            for (int i = 0; i < allArtistsList.Count; i++)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{mbArtistEndpoint}/id/{allArtistsList[i].Id}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyArt = JsonSerializer.Deserialize<MbArtistResource>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(allArtistsList[i].Name, verifyArt.Name);
            }
        }

        [Fact]
        public async Task MbArtist_FindByMbId()
        {
            // Add test data
            await SeedData(1, 1, 1, 5);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(mbArtistEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<MbArtistResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that the correct results are returned
            foreach (var targetElement in allElements)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{mbArtistEndpoint}?mbId={targetElement.MbId}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyElement = JsonSerializer.Deserialize<IEnumerable<MbArtistResource>>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Single(verifyElement);
                Assert.Equal(targetElement.Id, verifyElement.First().Id);
            }
        }


        [Fact]
        public async Task MbArtist_List()
        {
            // Add test data
            await SeedData(0, 0, 0, 5);
            int targetElementCount = 5;

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(mbArtistEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<MbArtistResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(targetElementCount, allElements.Count());
        }


        [Fact]
        public async Task MbArtist_Post()
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Add test data
            var artList = GenerateMbArtists(5);

            foreach (var art in artList)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new
                {
                    art.MbId,
                    art.Name,
                    art.OriginalName,
                    art.Include
                }), Encoding.UTF8, "application/json");

                var postResponse = await client.PostAsync(mbArtistEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<MbArtistResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(art.Name, postResponseObj.Name);
            }

            // Verify
            var httpResponse = await client.GetAsync(mbArtistEndpoint);
            httpResponse.EnsureSuccessStatusCode();

            // Deserialize and examine results.
            var verifyList = JsonSerializer.Deserialize<IEnumerable<MbArtistResource>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Single(verifyList.Where(x => x.Name.Equals(artList[0].Name)));
            Assert.Single(verifyList.Where(x => x.Name.Equals(artList[1].Name)));
        }

        [Fact]
        public async Task MbArtist_Delete()
        {
            // Add test data
            int seedCount = 3;
            await SeedData(1, 1, 1, seedCount);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            HttpResponseMessage allArtistsResponse = await client.GetAsync(mbArtistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtists = JsonSerializer.Deserialize<IEnumerable<MbArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();
            Assert.Equal(seedCount, allArtists.Count());

            var art0 = allArtists[0];
            var art1 = allArtists[1];
            var art2 = allArtists[2];

            // delete artist
            HttpResponseMessage art1DeleteResponse = await client.DeleteAsync($"{mbArtistEndpoint}/id/{art1.Id}");
            art1DeleteResponse.EnsureSuccessStatusCode();

            // verify correct response
            var art1DeleteObj = JsonSerializer.Deserialize<MbArtistResource>(await art1DeleteResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(art1.Id, art1DeleteObj.Id);

            // make sure that intended object was deleted and all other artists remain
            HttpResponseMessage remainingArtistsResponse = await client.GetAsync(mbArtistEndpoint);
            var remainingArtists = JsonSerializer.Deserialize<IEnumerable<MbArtistResource>>(await remainingArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(2, remainingArtists.Count());

            Assert.Single(remainingArtists.Where(x => x.Id.Equals(art0.Id)));
            Assert.Single(remainingArtists.Where(x => x.Id.Equals(art2.Id)));
        }

        [Fact]
        public async Task MbArtist_Update()
        {
            throw new NotImplementedException();
        }
    }
}
