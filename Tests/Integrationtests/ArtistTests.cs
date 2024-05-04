﻿using ArtistNormalizer.API;
using ArtistNormalizer.API.Domain.Models;
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
    public class ArtistTests : BaseIntegrationTests
    {
        public ArtistTests(CustomWebApplicationFactory<Startup> factory) : base(factory) { }

        [Fact]
        public async Task Artist_Post()
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Add test data
            var artList = GenerateArtists(2, 1, 2);

            foreach (var art in artList)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { art.Name }), Encoding.UTF8, "application/json");
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
            // Add test data
            int seedCount = 3;
            await SeedData(seedCount, 2, 1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            HttpResponseMessage allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtists = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();
            Assert.Equal(seedCount, allArtists.Count());

            var art0 = allArtists[0];
            var art1 = allArtists[1];
            var art2 = allArtists[2];

            // delete artist
            HttpResponseMessage art1DeleteResponse = await client.DeleteAsync($"{artistEndpoint}/id/{art1.Id}");
            art1DeleteResponse.EnsureSuccessStatusCode();

            // verify correct response
            var art1DeleteObj = JsonSerializer.Deserialize<ArtistResource>(await art1DeleteResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(art1.Id, art1DeleteObj.Id);

            // make sure that intended object was deleted and all other artists remain
            HttpResponseMessage remainingArtistsResponse = await client.GetAsync(artistEndpoint);
            var remainingArtists = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await remainingArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(2, remainingArtists.Count());

            Assert.Single(remainingArtists.Where(x => x.Id.Equals(art0.Id)));
            Assert.Single(remainingArtists.Where(x => x.Id.Equals(art2.Id)));
        }

        [Fact]
        public async Task Artist_List()
        {
            // Add test data
            await SeedData(3, 2, 2, 1);
            int targetElementCount = 3;

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(artistEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(targetElementCount, allElements.Count());
        }

        [Fact]
        public async Task Artist_FindById()
        {
            // Add test data
            await SeedData(5, 2, 1, 1);

            // Verify that an invalid id returns nothing
            HttpResponseMessage verifyNotFoundResponse = await client.GetAsync($"{artistEndpoint}/id/999999");
            verifyNotFoundResponse.EnsureSuccessStatusCode();

            var verifyNotFoundString = await verifyNotFoundResponse.Content.ReadAsStringAsync();
            Assert.Empty(verifyNotFoundString);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var allArtistsList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            for (int i = 0; i < allArtistsList.Count; i++)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{artistEndpoint}/id/{allArtistsList[i].Id}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyArt = JsonSerializer.Deserialize<ArtistResource>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(allArtistsList[i].Name, verifyArt.Name);
            }
        }

        [Fact]
        public async Task Artist_FindByName()
        {
            // Add test data
            await SeedData(2, 2, 5, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(artistEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that the correct results are returned
            foreach (var targetElement in allElements)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{artistEndpoint}?name={targetElement.Name}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyElement = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Single(verifyElement);
                Assert.Equal(targetElement.Id, verifyElement.First().Id);
            }
        }

        [Fact]
        public async Task Artist_Cleanup_After_Last_Alias_Removed()
        {
            // Add test data
            await SeedData(1, 2, 1, 1);

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
