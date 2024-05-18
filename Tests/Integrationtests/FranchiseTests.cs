using ArtistResolver.API;
using ArtistResolver.API.Resources;
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
    public class FranchiseTests : BaseIntegrationTests
    {
        public FranchiseTests(CustomWebApplicationFactory<Startup> factory) : base(factory) { }

        [Fact]
        public async Task Post()
        {
            // Add test data
            var fList = GenerateFranchise(3);
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var franchise in fList)
            {
                var jsonString = new StringContent(JsonSerializer.Serialize(new { franchise.Name }), Encoding.UTF8, "application/json");
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
        public async Task Delete()
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
        public async Task List()
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
        public async Task FindByName()
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
        public async Task FindById()
        {
            // Add test data
            await SeedData(1, 2, 5, 1);

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
        public async Task FindById_error_if_not_found()
        {
            // Add test data to ensure db is not empty
            await SeedData(1, 2, 5, 1);

            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Verify
            var invalidId = "999999";
            HttpResponseMessage verifyResponse = await client.GetAsync($"{franchiseEndpoint}/id/{invalidId}");

            // Expect a BadRequest due to duplicate entry
            Assert.Equal(System.Net.HttpStatusCode.NotFound, verifyResponse.StatusCode);
        }
    }
}
