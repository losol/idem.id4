using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Losol.Identity
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }

        public IConfiguration Configuration { get; }

        public IdentityServerConfig IdentityServerConfig { get; set; }

        public Startup(
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            this.Environment = environment;
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                });

            IdentityServerConfig = Configuration.GetSection("IdentityServer").Get<IdentityServerConfig>();
            switch (IdentityServerConfig.ConfigurationType)
            {
                case ConfigurationType.InMemory:
                    builder.AddInMemoryConfiguration(Configuration.GetSection("InMemoryConfiguration"));
                    break;

                case ConfigurationType.Database:
                    builder.AddDatabaseConfiguration(
                        Configuration.GetSection("DatabaseConfiguration"),
                        Configuration.GetConnectionString("DefaultConnection"));
                    break;
            }

            services.AddAuthentication();
            services.AddAuthorization();

            if (this.Environment.IsDevelopment())
            {
                // not recommended for production - you need to store your key material somewhere secure
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                var certCommonName = IdentityServerConfig.KeyStore.LocalMachine?.CommonName;
                if (string.IsNullOrEmpty(certCommonName))
                {
                    throw new InvalidOperationException("Certificate CommonName must be specified for use in production");
                }
                builder.AddSigningCredentialFromLocalMachineStorage(certCommonName);
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            if (this.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

            if (IdentityServerConfig.ConfigurationType == ConfigurationType.Database)
            {
                app.InitializeDatabase(Configuration.GetSection("InitialSeedData"));
            }
        }
    }
}
