using ArtistNormalizer.API;
using ArtistNormalizer.API.Domain.Models;
using ArtistNormalizer.API.Persistence.Contexts;
using ArtistNormalizer.API.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public class IntegrationTests : IClassFixture<CustomWebApplicationFactory<Startup>>, IAsyncLifetime
    {
        private readonly HttpClient client;
        private readonly CustomWebApplicationFactory<Startup> factory;
        private readonly AppDbContext dbContext;
        private const string franchiseEndpoint = "/api/franchise";
        private const string aliasEndpoint = "/api/alias";
        private const string artistEndpoint = "/api/artist";
        private const string mbArtistEndpoint = "/api/mbartist";

        public IntegrationTests(CustomWebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
            client = factory.CreateClient();
            dbContext = factory.Services.GetRequiredService<AppDbContext>();
        }

        public async Task InitializeAsync()
        {
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        private List<Franchise> GenerateFranchise(int franchiseCount)
        {
            var franchises = new List<Franchise>();
            for (int n = 0; n < franchiseCount; n++)
            {
                franchises.Add(new Franchise()
                {
                    Name = $"Franchise {n}"
                });
            }

            return franchises;
        }

        private List<Artist> GenerateArtists(int artistCount, int aliasCount, int franchiseCount)
        {
            var franchises = GenerateFranchise(franchiseCount);
            var artists = new List<Artist>();

            for (int i = 0; i < artistCount; i++)
            {
                var aliases = new List<Alias>();
                for (int x = 0; x < aliasCount; x++)
                {
                    foreach (var franchise in franchises)
                    {
                        var alias = new Alias()
                        {
                            Name = $"Artist {i} Alias {x} {franchise.Name}",
                            Franchise = franchise
                        };

                        aliases.Add(alias);
                    }
                }

                artists.Add(new Artist()
                {
                    Name = $"Artist {i}",
                    Aliases = aliases
                });
            }

            return artists;
        }

        private List<MbArtist> GenerateMbArtists(int mbArtistCount)
        {
            var mbArtists = new List<MbArtist>();

            for (int i = 0; i < mbArtistCount; i++)
            {
                var mbArtist = new MbArtist()
                {
                    MbId = $"MbId-{i}-6666-7777-8888-999999999999",
                    Name = $"MbArtist {i}",
                    OriginalName = $"Original MbArtist {i}",
                    Include = i % 2 == 0 // Alternate between true and false
                };

                mbArtists.Add(mbArtist);
            }

            return mbArtists;
        }

        private async Task SeedData(int artistCount, int aliasCount, int franchiseCount, int mbArtistCount)
        {
            var JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            List<Artist> artists = GenerateArtists(artistCount, aliasCount, franchiseCount);
            List<Franchise> franchises = artists
                .SelectMany(a => a.Aliases)
                .Select(al => al.Franchise)
                .Distinct()
                .ToList();

            List<MbArtist> mbArtists = GenerateMbArtists(mbArtistCount);


            foreach (var franchise in franchises)
            {
                FranchiseResource franchisePostResource = await PostFranchise(franchise);
                franchise.Id = franchisePostResource.Id;
            }

            foreach (var artist in artists)
            {
                ArtistResource artistPostResource = await PostArtist(artist);
                artist.Id = artistPostResource.Id;

                foreach (var alias in artist.Aliases)
                {
                    var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = alias.Name, artistid = artist.Id, franchiseId = alias.Franchise.Id }), Encoding.UTF8, "application/json");
                    HttpResponseMessage postResponse = await client.PostAsync(aliasEndpoint, jsonString);
                    postResponse.EnsureSuccessStatusCode();
                }
            }

            foreach (var artist in mbArtists)
            {
                MbArtistResource artistPostResource = await PostMbArtist(artist);
                artist.Id = artistPostResource.Id;
            }

        }

        private async Task<ArtistResource> PostArtist(Artist artist)
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var postJson = new StringContent(JsonSerializer.Serialize(new { Name = artist.Name }), Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(artistEndpoint, postJson);
            postResponse.EnsureSuccessStatusCode();

            ArtistResource postResource = JsonSerializer.Deserialize<ArtistResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
            return postResource;
        }

        private async Task<MbArtistResource> PostMbArtist(MbArtist artist)
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var postJson = new StringContent(JsonSerializer.Serialize(new { MbId = artist.MbId, Name = artist.Name, OriginalName = artist.OriginalName, Include = artist.Include }), Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(mbArtistEndpoint, postJson);
            postResponse.EnsureSuccessStatusCode();

            MbArtistResource postResource = JsonSerializer.Deserialize<MbArtistResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
            return postResource;
        }

        private async Task<FranchiseResource> PostFranchise(Franchise franchise)
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var postJson = new StringContent(JsonSerializer.Serialize(new { Name = franchise.Name }), Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(franchiseEndpoint, postJson);
            postResponse.EnsureSuccessStatusCode();

            FranchiseResource postResource = JsonSerializer.Deserialize<FranchiseResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
            return postResource;
        }

        [Fact]
        public async Task Artist_Post()
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Add test data
            var artList = GenerateArtists(2, 1, 2);

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
        public async Task Alias_Post()
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Create new aliases
            List<Artist> aliasList = GenerateArtists(1, 3, 2);

            Artist parentArtist = aliasList.First();
            ArtistResource parentArtistResource = await PostArtist(parentArtist);
            parentArtist.Id = parentArtistResource.Id;

            Franchise parentFranchise = parentArtist.Aliases.First().Franchise;
            FranchiseResource parentFranchiseResource = await PostFranchise(parentFranchise);
            parentFranchise.Id = parentFranchiseResource.Id;

            foreach (var alias in parentArtist.Aliases)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = alias.Name, artistid = parentArtist.Id, franchiseId = parentFranchise.Id }), Encoding.UTF8, "application/json");
                HttpResponseMessage postResponse = await client.PostAsync(aliasEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<AliasResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(alias.Name, postResponseObj.Name);
                Assert.Equal(parentArtist.Name, postResponseObj.Artist);
                Assert.Equal(parentFranchise.Name, postResponseObj.Franchise);
            }

            // Verify that all aliases have been created
            var aliasVerifyListResponse = await client.GetAsync(aliasEndpoint);
            aliasVerifyListResponse.EnsureSuccessStatusCode();

            var aliasVerifyList = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await aliasVerifyListResponse.Content.ReadAsStringAsync(), JsonOptions);

            foreach (var alias in parentArtist.Aliases)
            {
                Assert.Single(aliasVerifyList.Where(x => x.Name.Equals(alias.Name, StringComparison.InvariantCultureIgnoreCase)));
            }
        }

        [Fact]
        public async Task Alias_Post_Duplicate_Name_Different_Franchise()
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Create new aliases
            List<Artist> aliasList = GenerateArtists(1, 1, 2);
            string aliasName = "duplicateName";

            Artist parentArtist = aliasList.First();
            ArtistResource parentArtistResource = await PostArtist(parentArtist);
            parentArtist.Id = parentArtistResource.Id;

            foreach (var alias in parentArtist.Aliases)
            {
                FranchiseResource postFranchise = await PostFranchise(alias.Franchise);
                alias.Franchise.Id = postFranchise.Id;

                alias.Name = aliasName;
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = alias.Name, artistid = parentArtist.Id, franchiseId = alias.Franchise.Id }), Encoding.UTF8, "application/json");
                HttpResponseMessage postResponse = await client.PostAsync(aliasEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<AliasResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(alias.Name, postResponseObj.Name);
                Assert.Equal(parentArtist.Name, postResponseObj.Artist);
                Assert.Equal(alias.Franchise.Name, postResponseObj.Franchise);
            }

            // Verify that all aliases have been created
            var aliasVerifyListResponse = await client.GetAsync(aliasEndpoint);
            aliasVerifyListResponse.EnsureSuccessStatusCode();

            var aliasVerifyList = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await aliasVerifyListResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Single(aliasVerifyList.Select(a => a.Name).Distinct());
            Assert.Equal(2, aliasVerifyList.Select(a => a.FranchiseId).Distinct().Count());
        }

        [Fact]
        public async Task Alias_Post_Duplicate_Name_Same_Franchise()
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Create new aliases
            List<Artist> aliasList = GenerateArtists(1, 3, 1);
            string aliasName = "duplicatename";

            Artist parentArtist = aliasList.First();
            ArtistResource parentArtistResource = await PostArtist(parentArtist);
            parentArtist.Id = parentArtistResource.Id;

            Franchise parentFranchise = parentArtist.Aliases.First().Franchise;
            FranchiseResource parentFranchiseResource = await PostFranchise(parentFranchise);
            parentFranchise.Id = parentFranchiseResource.Id;

            foreach (var alias in parentArtist.Aliases)
            {
                alias.Name = aliasName;
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = alias.Name, artistid = parentArtist.Id, franchiseId = alias.Franchise.Id }), Encoding.UTF8, "application/json");
                HttpResponseMessage postResponse = await client.PostAsync(aliasEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<AliasResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(alias.Name, postResponseObj.Name);
                Assert.Equal(parentArtist.Name, postResponseObj.Artist);
                Assert.Equal(alias.Franchise.Name, postResponseObj.Franchise);
            }

            // Verify that all aliases have been created
            var aliasVerifyListResponse = await client.GetAsync(aliasEndpoint);
            aliasVerifyListResponse.EnsureSuccessStatusCode();

            var aliasVerifyList = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await aliasVerifyListResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Single(aliasVerifyList);
        }

        [Fact]
        public async Task Artist_Update()
        {
            throw new NotImplementedException();
        }


        [Fact]
        public async Task Alias_Delete()
        {
            // Add test data
            await SeedData(1, 3, 1, 1);
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
        public async Task Alias_List()
        {
            // Add test data
            await SeedData(2, 3, 2, 1);
            int targetElementCount = 2 * 3 * 2;

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(aliasEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(targetElementCount, allElements.Count());
        }

        [Fact]
        public async Task Alias_FindByAliasName()
        {
            // Add test data
            await SeedData(2, 3, 1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(aliasEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that the correct results are returned
            foreach (var targetElement in allElements)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{aliasEndpoint}?name={targetElement.Name}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyElement = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Single(verifyElement);
                Assert.Equal(targetElement.Id, verifyElement.First().Id);
            }
        }

        [Fact]
        public async Task Alias_FindByFranchiseName()
        {
            // Add test data
            await SeedData(2, 3, 1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage httpResponse = await client.GetAsync(franchiseEndpoint);
            httpResponse.EnsureSuccessStatusCode();
            var allFranchises = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            foreach (var franchise in allFranchises)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{aliasEndpoint}?franchise={franchise.Name}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyAlias = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(franchise.Aliases.Count(), verifyAlias.Count());
            }
        }

        [Fact]
        public async Task Alias_FindByAliasName_And_FranchiseName()
        {
            // Add test data
            await SeedData(1, 3, 3, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage httpResponse = await client.GetAsync(aliasEndpoint);
            httpResponse.EnsureSuccessStatusCode();
            var allAliases = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            foreach (var alias in allAliases)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{aliasEndpoint}?name={alias.Name}&franchise={alias.Franchise}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyAlias = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Single(verifyAlias);
                Assert.Equal(alias.Id, verifyAlias.First().Id);
            }
        }

        [Fact]
        public async Task Alias_FindById()
        {
            // Add test data
            await SeedData(2, 3, 1, 1);

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
        public async Task Franchise_Post()
        {
            // Add test data
            var fList = GenerateFranchise(3);
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var franchise in fList)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { Name = franchise.Name }), Encoding.UTF8, "application/json");
                HttpResponseMessage postResponse = await client.PostAsync(franchiseEndpoint, jsonString);
                postResponse.EnsureSuccessStatusCode();

                var postResponseObj = JsonSerializer.Deserialize<FranchiseResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(franchise.Name, postResponseObj.Name);
            }

            // Verify
            HttpResponseMessage httpResponse = await client.GetAsync(franchiseEndpoint);
            httpResponse.EnsureSuccessStatusCode();

            // Deserialize and examine results.
            var verifyList = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await httpResponse.Content.ReadAsStringAsync(), JsonOptions);

            Assert.Single(verifyList.Where(x => x.Name.Equals(fList[0].Name, StringComparison.InvariantCultureIgnoreCase)));
            Assert.Single(verifyList.Where(x => x.Name.Equals(fList[1].Name, StringComparison.InvariantCultureIgnoreCase)));
            Assert.Single(verifyList.Where(x => x.Name.Equals(fList[2].Name, StringComparison.InvariantCultureIgnoreCase)));
        }

        [Fact]
        public async Task Franchise_Delete()
        {
            // Add test data
            int seedCount = 3;
            await SeedData(1, 2, seedCount, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            HttpResponseMessage allFranchisesResponse = await client.GetAsync(franchiseEndpoint);
            allFranchisesResponse.EnsureSuccessStatusCode();
            var allFranchises = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await allFranchisesResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();
            Assert.Equal(seedCount, allFranchises.Count());

            var fr0 = allFranchises[0];
            var fr1 = allFranchises[1];
            var fr2 = allFranchises[2];

            // delete franchise
            HttpResponseMessage fr1DeleteResponse = await client.DeleteAsync($"{franchiseEndpoint}/id/{fr1.Id}");
            fr1DeleteResponse.EnsureSuccessStatusCode();

            // verify correct response
            var fr1DeleteObj = JsonSerializer.Deserialize<FranchiseResource>(await fr1DeleteResponse.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(fr1.Id, fr1DeleteObj.Id);

            // make sure that intended object was deleted and all other artists remain
            HttpResponseMessage remainingFranchisesResponse = await client.GetAsync(franchiseEndpoint);
            var remainingFranchises = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await remainingFranchisesResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(2, remainingFranchises.Count());

            Assert.Single(remainingFranchises.Where(x => x.Id.Equals(fr0.Id)));
            Assert.Single(remainingFranchises.Where(x => x.Id.Equals(fr2.Id)));
        }

        [Fact]
        public async Task Franchise_List()
        {
            // Add test data
            await SeedData(2, 3, 4, 1);
            int targetElementCount = 4;

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(franchiseEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            Assert.Equal(targetElementCount, allElements.Count());
        }

        [Fact]
        public async Task Franchise_FindByName()
        {
            // Add test data
            await SeedData(2, 2, 5, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allElementsResponse = await client.GetAsync(franchiseEndpoint);
            allElementsResponse.EnsureSuccessStatusCode();
            var allElements = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await allElementsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that the correct results are returned
            foreach (var targetElement in allElements)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{franchiseEndpoint}?name={targetElement.Name}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyElement = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Single(verifyElement);
                Assert.Equal(targetElement.Id, verifyElement.First().Id);
            }
        }

        [Fact]
        public async Task Franchise_FindById()
        {
            // Add test data
            await SeedData(1, 2, 5, 1);

            // Verify that an invalid id returns nothing
            HttpResponseMessage verifyNotFoundResponse = await client.GetAsync($"{franchiseEndpoint}/id/999999");
            verifyNotFoundResponse.EnsureSuccessStatusCode();

            var verifyNotFoundString = await verifyNotFoundResponse.Content.ReadAsStringAsync();
            Assert.Empty(verifyNotFoundString);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get Id of all elements
            HttpResponseMessage allFranchisesResponse = await client.GetAsync(franchiseEndpoint);
            allFranchisesResponse.EnsureSuccessStatusCode();
            var allFranchisesList = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await allFranchisesResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify that findById returns correct results
            for (int i = 0; i < allFranchisesList.Count; i++)
            {
                HttpResponseMessage verifyResponse = await client.GetAsync($"{franchiseEndpoint}/id/{allFranchisesList[i].Id}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifyFranchise = JsonSerializer.Deserialize<FranchiseResource>(await verifyResponse.Content.ReadAsStringAsync(), JsonOptions);
                Assert.Equal(allFranchisesList[i].Name, verifyFranchise.Name);
            }
        }

        [Fact]
        public async Task Alias_Cleanup_After_Parent_Artist_Removed()
        {
            // Add test data
            await SeedData(1, 2, 1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            HttpResponseMessage allArtistsResponse = await client.GetAsync(artistEndpoint);
            allArtistsResponse.EnsureSuccessStatusCode();
            var artistList = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await allArtistsResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify original object count
            Assert.Single(artistList);
            var deleteArtist = artistList[0];
            Assert.Equal(2, deleteArtist.Aliases.Count());

            // delete artist
            HttpResponseMessage deleteArtistRequest = await client.DeleteAsync($"{artistEndpoint}/id/{deleteArtist.Id}");
            deleteArtistRequest.EnsureSuccessStatusCode();

            // verify correct response
            var deleteArtistResponse = JsonSerializer.Deserialize<ArtistResource>(await deleteArtistRequest.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(deleteArtist.Id, deleteArtistResponse.Id);

            // verify artists were deleted
            HttpResponseMessage checkRemainingArtists = await client.GetAsync(artistEndpoint);
            checkRemainingArtists.EnsureSuccessStatusCode();
            var checkArtist = JsonSerializer.Deserialize<IEnumerable<ArtistResource>>(await checkRemainingArtists.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkArtist);

            // verify aliases were deleted
            HttpResponseMessage checkRemainingAliases = await client.GetAsync(aliasEndpoint);
            checkRemainingAliases.EnsureSuccessStatusCode();
            var checkAlias = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await checkRemainingAliases.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkAlias);
        }

        [Fact]
        public async Task Alias_Cleanup_After_Parent_Franchise_Removed()
        {
            // Add test data
            await SeedData(1, 2, 1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // get all elements
            HttpResponseMessage allFranchisesResponse = await client.GetAsync(franchiseEndpoint);
            allFranchisesResponse.EnsureSuccessStatusCode();
            var franchisesList = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await allFranchisesResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify original object count
            Assert.Single(franchisesList);
            var deleteFranchise = franchisesList[0];
            Assert.Equal(2, deleteFranchise.Aliases.Count());

            // delete franchise
            HttpResponseMessage deleteFranchiseRequest = await client.DeleteAsync($"{franchiseEndpoint}/id/{deleteFranchise.Id}");
            deleteFranchiseRequest.EnsureSuccessStatusCode();

            // verify correct response
            var deleteFranchiseResponse = JsonSerializer.Deserialize<FranchiseResource>(await deleteFranchiseRequest.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(deleteFranchise.Id, deleteFranchiseResponse.Id);

            // verify artists were deleted
            HttpResponseMessage checkRemaningFranchises = await client.GetAsync(franchiseEndpoint);
            checkRemaningFranchises.EnsureSuccessStatusCode();
            var checkFranchise = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await checkRemaningFranchises.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkFranchise);

            // verify aliases were deleted
            HttpResponseMessage checkRemainingAliases = await client.GetAsync(aliasEndpoint);
            checkRemainingAliases.EnsureSuccessStatusCode();
            var checkAlias = JsonSerializer.Deserialize<IEnumerable<AliasResource>>(await checkRemainingAliases.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkAlias);
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

        [Fact]
        public async Task Franchise_Cleanup_After_Last_Alias_Removed()
        {
            await SeedData(1, 2, 1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            HttpResponseMessage allFranchisesResponse = await client.GetAsync(franchiseEndpoint);
            allFranchisesResponse.EnsureSuccessStatusCode();
            var franchiseList = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await allFranchisesResponse.Content.ReadAsStringAsync(), JsonOptions)
                .ToList();

            // verify original object count
            Assert.Single(franchiseList);

            var franchise = franchiseList.First();
            Assert.Equal(2, franchise.Aliases.Count());

            // delete aliases
            foreach (var alias in franchise.Aliases)
            {
                var deleteResponse = await client.DeleteAsync($"{aliasEndpoint}/id/{alias.Id}");
                deleteResponse.EnsureSuccessStatusCode();
            }

            // verify franchises were deleted
            HttpResponseMessage checkRemainingFranchises = await client.GetAsync(franchiseEndpoint);
            checkRemainingFranchises.EnsureSuccessStatusCode();
            var checkFranchise = JsonSerializer.Deserialize<IEnumerable<FranchiseResource>>(await checkRemainingFranchises.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Empty(checkFranchise);
        }

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
                    MbId = art.MbId,
                    Name = art.Name,
                    OriginalName = art.OriginalName,
                    Include = art.Include
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

