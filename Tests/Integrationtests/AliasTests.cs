using ArtistNormalizer.API;
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
    public class AliasTests : BaseIntegrationTests
    {
        public AliasTests(CustomWebApplicationFactory<Startup> factory) : base(factory) { }

        [Fact]
        public async Task Post()
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
                var jsonString = new StringContent(JsonSerializer.Serialize(new { alias.Name, artistid = parentArtist.Id, franchiseId = parentFranchise.Id }), Encoding.UTF8, "application/json");
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
        public async Task Post_Duplicate_Name_Different_Franchise()
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
                var jsonString = new StringContent(JsonSerializer.Serialize(new { alias.Name, artistid = parentArtist.Id, franchiseId = alias.Franchise.Id }), Encoding.UTF8, "application/json");
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
        public async Task Post_Duplicate_Name_Same_Franchise()
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Create new aliases
            List<Artist> aliasList = GenerateArtists(1, 1, 1);
            string aliasName = "duplicatename";

            Artist parentArtist = aliasList.First();
            ArtistResource parentArtistResource = await PostArtist(parentArtist);
            parentArtist.Id = parentArtistResource.Id;

            Franchise parentFranchise = parentArtist.Aliases.First().Franchise;
            FranchiseResource parentFranchiseResource = await PostFranchise(parentFranchise);
            parentFranchise.Id = parentFranchiseResource.Id;

            var postAlias = parentArtist.Aliases.First();

            postAlias.Name = aliasName;
            var jsonString = new StringContent(JsonSerializer.Serialize(new { postAlias.Name, artistid = parentArtist.Id, franchiseId = postAlias.Franchise.Id }), Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(aliasEndpoint, jsonString);
            postResponse.EnsureSuccessStatusCode();


            HttpResponseMessage failurePostResponse = await client.PostAsync(aliasEndpoint, jsonString);
            Assert.Equal(System.Net.HttpStatusCode.Conflict, failurePostResponse.StatusCode);
        }

        [Fact]
        public async Task Delete()
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
        public async Task List()
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
        public async Task FindByAliasName()
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
        public async Task FindByFranchiseName()
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
        public async Task FindByAliasName_And_FranchiseName()
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
        public async Task FindById()
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
        public async Task FindById_error_if_not_found()
        {
            // Add test data to ensure db is not empty
            await SeedData(2, 3, 1, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Verify
            var invalidId = "999999";
            HttpResponseMessage verifyResponse = await client.GetAsync($"{aliasEndpoint}/id/{invalidId}");

            // Expect a BadRequest due to duplicate entry
            Assert.Equal(System.Net.HttpStatusCode.NotFound, verifyResponse.StatusCode);
        }

        [Fact]
        public async Task Cleanup_After_Parent_Artist_Removed()
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
        public async Task Cleanup_After_Parent_Franchise_Removed()
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
    }
}