using ArtistResolver.API;
using ArtistResolver.API.Domain.Models;
using ArtistResolver.API.Persistence.Contexts;
using ArtistResolver.API.Resources;
using Microsoft.Extensions.DependencyInjection;
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
    public abstract class BaseIntegrationTests : IClassFixture<CustomWebApplicationFactory<Startup>>, IAsyncLifetime
    {
        protected readonly HttpClient client;
        protected readonly CustomWebApplicationFactory<Startup> factory;
        protected readonly AppDbContext dbContext;
        protected const string franchiseEndpoint = "/api/franchise";
        protected const string aliasEndpoint = "/api/alias";
        protected const string artistEndpoint = "/api/artist";
        protected const string mbArtistEndpoint = "/api/mbartist";

        public BaseIntegrationTests(CustomWebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
            client = factory.CreateClient();
            dbContext = factory.Services.GetRequiredService<AppDbContext>();
        }

        public async Task InitializeAsync()
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        internal List<Franchise> GenerateFranchise(int franchiseCount)
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

        internal List<Artist> GenerateArtists(int artistCount, int aliasCount, int franchiseCount)
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

        internal List<MbArtist> GenerateMbArtists(int mbArtistCount)
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

        internal async Task SeedData(int artistCount, int aliasCount, int franchiseCount, int mbArtistCount)
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
                    var jsonString = new StringContent(JsonSerializer.Serialize(new { alias.Name, artistid = artist.Id, franchiseId = alias.Franchise.Id }), Encoding.UTF8, "application/json");
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

        internal async Task<ArtistResource> PostArtist(Artist artist)
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var postJson = new StringContent(JsonSerializer.Serialize(new { artist.Name }), Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(artistEndpoint, postJson);
            postResponse.EnsureSuccessStatusCode();

            ArtistResource postResource = JsonSerializer.Deserialize<ArtistResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
            return postResource;
        }

        internal async Task<MbArtistResource> PostMbArtist(MbArtist artist)
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var postJson = new StringContent(JsonSerializer.Serialize(new { artist.MbId, artist.Name, artist.OriginalName, artist.Include }), Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(mbArtistEndpoint, postJson);
            postResponse.EnsureSuccessStatusCode();

            MbArtistResource postResource = JsonSerializer.Deserialize<MbArtistResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
            return postResource;
        }

        internal async Task<FranchiseResource> PostFranchise(Franchise franchise)
        {
            var JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var postJson = new StringContent(JsonSerializer.Serialize(new { franchise.Name }), Encoding.UTF8, "application/json");
            HttpResponseMessage postResponse = await client.PostAsync(franchiseEndpoint, postJson);
            postResponse.EnsureSuccessStatusCode();

            FranchiseResource postResource = JsonSerializer.Deserialize<FranchiseResource>(await postResponse.Content.ReadAsStringAsync(), JsonOptions);
            return postResource;
        }


    }
}
