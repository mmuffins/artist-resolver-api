using ArtistResolver.API.Domain.Repositories;
using ArtistResolver.API.Domain.Services;
using ArtistResolver.API.Mapping;
using ArtistResolver.API.Persistence.Contexts;
using ArtistResolver.API.Persistence.Repositories;
using ArtistResolver.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;

namespace ArtistResolver.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Console.OutputEncoding = Encoding.UTF8;
            services.AddMvc()
                .AddControllersAsServices();

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "unittest")
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("artistnormalizer-api-in-memory");
                });
            }
            else
            {
                string dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? Configuration.GetSection("Settings").GetSection("DBPath").Value;
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite("Data Source=" + dbPath);
                });
            }

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IArtistRepository, ArtistRepository>();
            services.AddScoped<IArtistService, ArtistService>();
            services.AddScoped<IFranchiseRepository, FranchiseRepository>();
            services.AddScoped<IFranchiseService, FranchiseService>();
            services.AddScoped<IAliasRepository, AliasRepository>();
            services.AddScoped<IAliasService, AliasService>();
            services.AddScoped<IMbArtistRepository, MbArtistRepository>();
            services.AddScoped<IMbArtistService, MbArtistService>();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<ModelToResourceProfile>();
                cfg.AddProfile<ResourceToModelProfile>();
            });

            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("Database", tags: new[] { "db", "all" });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
                endpoints.MapHealthChecks("/health/db", new HealthCheckOptions
                {
                    Predicate = (check) => check.Tags.Contains("db")
                });
            });
        }
    }
}
