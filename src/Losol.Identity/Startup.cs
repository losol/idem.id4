using Losol.Communication.Sms.Mock;
using Losol.Communication.Sms.Twilio;
using Losol.Identity.Config;
using Losol.Identity.Data;
using Losol.Identity.Extensions;
using Losol.Identity.Model;
using Losol.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AspNetCore.Mvc.Razor;

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
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureLocalization(Configuration.GetDefaultCulture());
            services.AddControllersWithViews();
            services.AddRazorPages()
                .AddRazorRuntimeCompilation()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;

                    // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                    options.EmitStaticAudienceClaim = true;
                })
                .AddAspNetIdentity<ApplicationUser>();

            IdentityServerConfig = Configuration.GetSection("IdentityServer").Get<IdentityServerConfig>();
            switch (IdentityServerConfig.ConfigurationType)
            {
                case ConfigurationType.InMemory:
                    builder.AddInMemoryConfiguration(Configuration.GetSection("InMemoryConfiguration"));
                    break;

                case ConfigurationType.Database:
                    builder.AddDatabaseConfiguration(
                        Configuration.GetSection("DatabaseConfiguration"),
                        connectionString);
                    break;
            }

            services.AddAuthentication();
            services.AddAuthorization();
            services.AddIdemServices();

            if (Environment.IsDevelopment())
            {
                services.AddMockSmsServices();
            }
            else
            {
                services.AddTwilioSmsServices(Configuration.GetSection("Twilio"));
            }

            if (Environment.IsDevelopment())
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
            app.InitializeLocalization(
                Configuration.GetSupportedCultures(),
                Configuration.GetDefaultCulture(),
                Environment);

            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
            });

            if (!bool.TrueString.Equals(Configuration["SkipDbInitialization"]))
            {
                app.InitializeDatabase(
                    IdentityServerConfig.ConfigurationType,
                    Configuration.GetSection("InitialSeedData"));
            }
        }
    }
}
