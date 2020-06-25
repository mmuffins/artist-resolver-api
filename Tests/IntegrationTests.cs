using ArtistNormalizer.API;
using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Resources;
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
            var artistList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await httpResponse.Content.ReadAsStringAsync(), jsonOptions);

            foreach (var artist in artistList)
            {
                var deleteResponse = await client.DeleteAsync($"{artistEndpoint}/id/{artist.Id}");
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
            var JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var aliasEndpoint = "/api/alias";

            var artists = GenerateArtists(artistCount, aliasCount);

            foreach (var artist in artists)
            {
                foreach (var alias in artist.Aliases)
                {
                    var jsonString = new StringContent(JsonSerializer.Serialize(new { Alias = alias.Name, Artist = artist.Name }), Encoding.UTF8, "application/json");
                    var postResponse = await client.PostAsync(aliasEndpoint, jsonString);
                    postResponse.EnsureSuccessStatusCode();
                }
            }
        }

        [Fact]
        public async Task Artist_Post()
        {
            await Cleanup();

            var artistEndpoint = "/api/artist";

            // Add test data
            var artList = GenerateArtists(2, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var art in artList)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = art.Name }), Encoding.UTF8, "application/json");
                var postResponse = await client.PostAsync(artistEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<ArtistResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(art.Name, postResponseObj.Name);
            }

            // Verify
            var httpResponse = await client.GetAsync(artistEndpoint);
            httpResponse.EnsureSuccessStatusCode();

            // Deserialize and examine results.
            var verifyList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Single(verifyList.Where(x => x.Name.Equals(artList[0].Name)));
            Assert.Single(verifyList.Where(x => x.Name.Equals(artList[1].Name)));
        }

        [Fact]
        public async Task Artist_Delete()
        {
            var artistEndpoint = "/api/artist";

            // Add test data
            await Cleanup();
            int seedCount = 3;
            await SeedData(seedCount, 2);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            var allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtists = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();
            Assert.Equal(seedCount, allArtists.Count());

            var art0 = allArtists[0];
            var art1 = allArtists[1];
            var art2 = allArtists[2];

            // delete artist
            var art1DeleteResponse = await client.DeleteAsync($"{artistEndpoint}/id/{art1.Id}");
            art1DeleteResponse.EnsureSuccessStatusCode();

            // verify correct response
            var art1DeleteObj = JsonSerializer.Deserialize<ArtistResource>(await art1DeleteResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(art1.Id, art1DeleteObj.Id);

            // make sure that intended object was deleted and all other artists remain
            var remainingArtistsResponse = await client.GetAsync(artistEndpoint);
            var remainingArtists = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await remainingArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(2, remainingArtists.Count());

            Assert.Single(remainingArtists.Where(x => x.Id.Equals(art0.Id)));
            Assert.Single(remainingArtists.Where(x => x.Id.Equals(art2.Id)));
        }

        [Fact]
        public async Task Artist_FindById()
        {
            var artistEndpoint = "/api/artist";

            // Add test data
            await Cleanup();
            await SeedData(5,2);

            // Verify that an invalid id returns nothing
            var verifyNotFoundResponse = await client.GetAsync($"{artistEndpoint}/id/999999");
            verifyNotFoundResponse.EnsureSuccessStatusCode();

            var verifyNotFoundString = await verifyNotFoundResponse.Content.ReadAsStringAsync();
            Assert.Empty(verifyNotFoundString);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            var allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtistsList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            for (int i = 0; i < allArtistsList.Count; i++)
            {
                var verifyResponse = await client.GetAsync($"{artistEndpoint}/id/{allArtistsList[i].Id}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyArt = JsonSerializer.Deserialize<ArtistResource>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(allArtistsList[i].Name, verifyArt.Name);
            }
        }

        [Fact]
        public async Task Artist_FindByName()
        {
            var artistEndpoint = "/api/artist";

            // Add test data
            await Cleanup();
            await SeedData(5, 2);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            var allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtistsList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            for (int i = 0; i < allArtistsList.Count; i++)
            {
                var verifyResponse = await client.GetAsync($"{artistEndpoint}/name/{allArtistsList[i].Name}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyArt = JsonSerializer.Deserialize<ArtistResource>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(allArtistsList[i].Id, verifyArt.Id);
            }
        }

        [Fact]
        public async Task Alias_Post_New()
        {
            await Cleanup();
            var aliasEndpoint = "/api/alias";

            // Create new aliases
            var aliasList = GenerateArtists(1, 3);
            var parentArtist = aliasList.First();

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var alias in parentArtist.Aliases)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Alias = alias.Name, Artist = parentArtist.Name }), Encoding.UTF8, "application/json");
                var postResponse = await client.PostAsync(aliasEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<AliasResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(alias.Name, postResponseObj.Name);
                Assert.Equal(parentArtist.Name, postResponseObj.Artist);
            }

            // Verify that all aliases have been created
            var aliasVerifyListResponse = await client.GetAsync(aliasEndpoint);
            aliasVerifyListResponse.EnsureSuccessStatusCode();

            var aliasVerifyList = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await aliasVerifyListResponse.Content.ReadAsStringAsync(), JsonOptions);

            foreach (var alias in parentArtist.Aliases)
            {
                Assert.Single(aliasVerifyList.Where(x => x.Name.Equals(alias.Name)));
            }
        }

        [Fact]
        public async Task Alias_Post_AddToExisting()
        {
            var aliasEndpoint = "/api/alias";

            // Add test data
            await Cleanup();
            var artistList = GenerateArtists(1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var artist = artistList[0];
            var originalAlias = artist.Aliases[0];

            var jsonString = new StringContent(JsonSerializer.Serialize(new { Alias = originalAlias.Name, Artist = artist.Name }), Encoding.UTF8, "application/json");
            var postResponse = await client.PostAsync(aliasEndpoint, jsonString);
            postResponse.EnsureSuccessStatusCode();

            // Add additional alias
            var additionalAlias = new Alias()
            {
                Name = "New Alias"
            };
            var addJsonString = new StringContent(JsonSerializer.Serialize(new { Alias = additionalAlias.Name, Artist = artist.Name }), Encoding.UTF8, "application/json");
            var addPostResponse = await client.PostAsync(aliasEndpoint, addJsonString);
            addPostResponse.EnsureSuccessStatusCode();

            // verify that alias was added to artist
            var aliasVerifyListResponse = await client.GetAsync(aliasEndpoint);
            aliasVerifyListResponse.EnsureSuccessStatusCode();

            var aliasVerifyList = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await aliasVerifyListResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Single(aliasVerifyList.Where(x => x.Name.Equals(originalAlias.Name)));
            Assert.Single(aliasVerifyList.Where(x => x.Name.Equals(additionalAlias.Name)));
        }

        [Fact]
        public async Task Alias_Delete()
        {
            // Add test data
            await Cleanup();
            await SeedData(1, 3);
            var aliasEndpoint = "/api/alias";
            var artistEndpoint = "/api/artist";

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            var allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var artistList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify original object count
            Assert.Single(artistList);

            var artist = artistList[0];
            Assert.Equal(3, artist.Aliases.Count());

            var keepAlias1 = artist.Aliases[0];
            var keepAlias2 = artist.Aliases[2];
            var deleteAlias = artist.Aliases[1];

            // delete alias
            var deleteResponse = await client.DeleteAsync($"{aliasEndpoint}/id/{deleteAlias.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            // verify correct response
            var deleteResponseObj = JsonSerializer.Deserialize<AliasResource>(await deleteResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(deleteAlias.Id, deleteResponseObj.Id);

            // verify remaining aliases
            var checkArtistsResponse = await client.GetAsync(artistEndpoint);
            checkArtistsResponse.EnsureSuccessStatusCode();
            var checkArtist = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await checkArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .First();

            Assert.Single(checkArtist.Aliases.Where(x => x.Id == keepAlias1.Id));
            Assert.Single(checkArtist.Aliases.Where(x => x.Id == keepAlias2.Id));
        }

        [Fact]
        public async Task Alias_FindByName()
        {
            var aliasEndpoint = "/api/alias";

            // Add test data
            await Cleanup();
            await SeedData(2, 3);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            var allAliasesResponse = await client.GetAsync(aliasEndpoint);
            allAliasesResponse.EnsureSuccessStatusCode();
            var allAliases = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allAliasesResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            for (int i = 0; i < allAliases.Count; i++)
            {
                var verifyResponse = await client.GetAsync($"{aliasEndpoint}/name/{allAliases[i].Name}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyAlias = JsonSerializer.Deserialize<AliasResource>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(allAliases[i].Id, verifyAlias.Id);
            }
        }

        [Fact]
        public async Task Alias_FindById()
        {
            var aliasEndpoint = "/api/alias";

            // Add test data
            await Cleanup();
            await SeedData(2, 3);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            var allAliasesResponse = await client.GetAsync(aliasEndpoint);
            allAliasesResponse.EnsureSuccessStatusCode();
            var allAliases = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allAliasesResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            for (int i = 0; i < allAliases.Count; i++)
            {
                var verifyResponse = await client.GetAsync($"{aliasEndpoint}/id/{allAliases[i].Id}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyAlias = JsonSerializer.Deserialize<AliasResource>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(allAliases[i].Name, verifyAlias.Name);
            }
        }

        [Fact]
        public async Task Alias_Cleanup_After_Parent_Artist_Removed()
        {
            // Add test data
            await Cleanup();
            await SeedData(1, 2);
            var aliasEndpoint = "/api/alias";
            var artistEndpoint = "/api/artist";

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            var allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var artistList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify original object count
            Assert.Single(artistList);
            var deleteArtist = artistList[0];
            Assert.Equal(2, deleteArtist.Aliases.Count());

            // delete artist
            var deleteArtistRequest = await client.DeleteAsync($"{artistEndpoint}/id/{deleteArtist.Id}");
            deleteArtistRequest.EnsureSuccessStatusCode();

            // verify correct response
            var deleteArtistResponse = JsonSerializer.Deserialize<ArtistResource>(await deleteArtistRequest.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(deleteArtist.Id, deleteArtistResponse.Id);

            // verify artist were deleted
            var checkRemainingArtists = await client.GetAsync(artistEndpoint);
            checkRemainingArtists.EnsureSuccessStatusCode();
            var checkArtist = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await checkRemainingArtists.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkArtist);

            // verify aliases were deleted
            var checkRemainingAliases = await client.GetAsync(aliasEndpoint);
            checkRemainingAliases.EnsureSuccessStatusCode();
            var checkAlias = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await checkRemainingAliases.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkAlias);
        }

        [Fact]
        public async Task Artist_Cleanup_After_Last_Alias_Removed()
        {
            // Add test data
            await Cleanup();
            await SeedData(1, 2);
            var aliasEndpoint = "/api/alias";
            var artistEndpoint = "/api/artist";

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            var allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var artistList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify original object count
            Assert.Single(artistList);

            var artist = artistList[0];
            Assert.Equal(2, artist.Aliases.Count());


            // delete aliases

            foreach (var alias in artist.Aliases)
            {
                var deleteResponse = await client.DeleteAsync($"{aliasEndpoint}/id/{alias.Id}");
                deleteResponse.EnsureSuccessStatusCode();
            }

            // verify aliases were deleted
            var checkRemainingAliases = await client.GetAsync(aliasEndpoint);
            checkRemainingAliases.EnsureSuccessStatusCode();
            var checkAlias = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await checkRemainingAliases.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkAlias);

            // verify artist were deleted
            var checkRemainingArtists = await client.GetAsync(artistEndpoint);
            checkRemainingArtists.EnsureSuccessStatusCode();
            var checkArtist = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await checkRemainingArtists.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkArtist);

        }


    }
}

