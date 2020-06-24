using ArtistNormalizer.API;
using ArtistNormalizer.API.Domain.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        private async Task Cleanup()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var artistEndpoint = "/api/artist";
            var httpResponse = await client.GetAsync(artistEndpoint);
            httpResponse.EnsureSuccessStatusCode();
            var artistList = JsonSerializer.Deserialize<IEnumerable<Artist>>(await httpResponse.Content.ReadAsStringAsync(), jsonOptions);

            foreach (var artist in artistList)
            {
                var deleteResponse = await client.DeleteAsync($"{artistEndpoint}/{artist.Id}");
                deleteResponse.EnsureSuccessStatusCode();
            }


            var checkEmptyArtist = await client.GetAsync(artistEndpoint);
            checkEmptyArtist.EnsureSuccessStatusCode();

            var checkEmptyArtistObj = JsonSerializer.Deserialize<IEnumerable<Artist>>(await checkEmptyArtist.Content.ReadAsStringAsync(), jsonOptions);
            if(checkEmptyArtistObj.Count() > 0)
            {
                throw new Exception("Artist cleanup not successful.");
            }
        }

        private List<Artist> GenerateArtists(int artistCount, int aliasCount)
        {
            var artists = new List<Artist>();

            for (int i = 0; i < artistCount; i++)
            {
                var aliases = new List<Alias>();
                for (int x = 0; x < aliasCount; x++)
                {
                    aliases.Add(new Alias()
                    {
                        Name = $"Artist {i} Alias {x}"
                    });
                }

                artists.Add(new Artist()
                {
                    Name = $"Artist {i}",
                    Aliases = aliases
                });
            }

            return artists;
        }

        private async Task SeedData(int artistCount, int aliasCount)
        {
            var aritsts = GenerateArtists(artistCount, aliasCount);

        }

        [Fact]
        public async Task Artist_Post()
        {
            await Cleanup();

            var artistEndpoint = "/api/artist";

            // Add test data
            var artList = GenerateArtists(2, 1);

            var JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            foreach (var art in artList)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = art.Name }), Encoding.UTF8, "application/json");
                var postResponse = await client.PostAsync(artistEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<Artist>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(art.Name, postResponseObj.Name);
            }

            // Verify
            var httpResponse = await client.GetAsync(artistEndpoint);
            httpResponse.EnsureSuccessStatusCode();

            // Deserialize and examine results.
            var verifyList = JsonSerializer.Deserialize<IEnumerable<Artist>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Collection(verifyList, item => Assert.Contains(artList[0].Name, item.Name),
                                            item => Assert.Contains(artList[1].Name, item.Name));
        }

        [Fact]
        public async Task Artist_Delete()
        {
            var artistEndpoint = "/api/artist";
            var initialResponse = await client.GetAsync(artistEndpoint);
            initialResponse.EnsureSuccessStatusCode();

            var JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            // Get artists from previous test and delete all but one
            var initialArtist = JsonSerializer.Deserialize<IEnumerable<Artist>>(await initialResponse.Content.ReadAsStringAsync(), JsonOptions).ToList();
            Assert.Equal(2, initialArtist.Count());

            var deleteArtist = initialArtist[0];
            var keepArtist = initialArtist[1];

            var deleteResponse = await client.DeleteAsync($"{artistEndpoint}/id/{deleteArtist.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            var deleteResponseObj = JsonSerializer.Deserialize<Artist>(await deleteResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(deleteArtist.Id, deleteResponseObj.Id);

            // Verify
            var httpResponse = await client.GetAsync(artistEndpoint);
            httpResponse.EnsureSuccessStatusCode();
            var verifyList = JsonSerializer.Deserialize<IEnumerable<Artist>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions).ToList();

            Assert.Single(verifyList);
            Assert.Equal(keepArtist.Id, verifyList.First().Id);

            //// Delete last artist
            //var deleteLastResponse = await client.DeleteAsync($"{artistEndpoint}/{keepArtist.Id}");
            //deleteLastResponse.EnsureSuccessStatusCode();

            //var emptyResponse = await client.GetAsync(artistEndpoint);
            //emptyResponse.EnsureSuccessStatusCode();

            //var emptyResponseObj = JsonSerializer.Deserialize<IEnumerable<Artist>>(await emptyResponse.Content.ReadAsStringAsync(), JsonOptions);
            //Assert.Empty(emptyResponseObj);
        }

        [Fact]
        public async Task Artist_FindById()
        {
            await Cleanup();

            var artistEndpoint = "/api/artist";

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
                var postResponse = await client.PostAsync(artistEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();
            }

            // Verify that an invalid id returns nothing

            var verifyNotFoundResponse = await client.GetAsync($"{artistEndpoint}/id/999999");
            verifyNotFoundResponse.EnsureSuccessStatusCode();

            var verifyNotFoundString = await verifyNotFoundResponse.Content.ReadAsStringAsync();
            //var verifyNotList = JsonSerializer.Deserialize<IEnumerable<Artist>>(await verifyNotFoundResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(verifyNotFoundString);

            // get Id of all elements
            var allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtistsList = JsonSerializer.Deserialize<IEnumerable<Artist>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            var verifyArt1Response = await client.GetAsync($"{artistEndpoint}/id/{allArtistsList[0].Id}");
            verifyArt1Response.EnsureSuccessStatusCode();

            var dde = await verifyArt1Response.Content.ReadAsStringAsync();

            var verifyArt1 = JsonSerializer.Deserialize<Artist>(await verifyArt1Response.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(allArtistsList[0].Name, verifyArt1.Name);

            var verifyArt2Response = await client.GetAsync($"{artistEndpoint}/id/{allArtistsList[1].Id}");
            verifyArt2Response.EnsureSuccessStatusCode();

            var verifyArt2 = JsonSerializer.Deserialize<Artist>(await verifyArt2Response.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(allArtistsList[1].Name, verifyArt2.Name);
        }

        [Fact]
        public async Task Artist_FindByName()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Alias_Post_New()
        {
            await Cleanup();
            var aliasEndpoint = "/api/alias";

            // Create new aliases
            var aliasList = GenerateArtists(1, 2);
            var parentArtist = aliasList.First();

            var JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            foreach (var alias in parentArtist.Aliases)
            {
                var dde = JsonSerializer.Serialize(new { Alias = alias.Name, Artist = parentArtist.Name });
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Alias = alias.Name, Artist = parentArtist.Name }), Encoding.UTF8, "application/json");
                var postResponse = await client.PostAsync(aliasEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<Alias>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(alias.Name, postResponseObj.Name);
                Assert.Equal(alias.Artist.Name, postResponseObj.Artist.Name);
            }

            // Verify that all aliases have been created
            var aliasVerifyListResponse = await client.GetAsync(aliasEndpoint);
            aliasVerifyListResponse.EnsureSuccessStatusCode();

            var aliasVerifyList = JsonSerializer.Deserialize<IEnumerable<Alias>>(await aliasVerifyListResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Collection(aliasVerifyList, item => Assert.Contains(parentArtist.Aliases[0].Name, item.Name),
                                            item => Assert.Contains(parentArtist.Aliases[1].Name, item.Name));
        }

        [Fact]
        public async Task Alias_Post_AddToExisting()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Alias_Delete()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Alias_FindByName()
        {
            throw new NotImplementedException();
        }
    }
}

