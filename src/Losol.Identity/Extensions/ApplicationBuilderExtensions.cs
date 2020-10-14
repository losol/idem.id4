using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Losol.Identity.Config;
using Losol.Identity.Data;
using Losol.Identity.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace Losol.Identity.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void InitializeDatabase(this IApplicationBuilder app,
            ConfigurationType configurationType,
            IConfiguration seedDataConfig = null)
        {
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            InitializeApplicationDbContext(serviceScope);
            if (configurationType == ConfigurationType.Database)
            {
                InitializeConfigurationDbContext(seedDataConfig, serviceScope);
            }
        }

        public static void InitializeLocalization(
            this IApplicationBuilder app,
            CultureInfo[] supportedCultures,
            CultureInfo defaultCulture,
            IWebHostEnvironment environment)
        {
            var requestCultureProviders = new List<IRequestCultureProvider>
            {
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider()
            };

            // Use localization by language header only in development environment
            if (environment.IsDevelopment())
            {
                requestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
            }

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(defaultCulture),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                RequestCultureProviders = requestCultureProviders
            });
        }

        private static void InitializeApplicationDbContext(IServiceScope serviceScope)
        {
            serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

            var userMgr = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = userMgr.FindByNameAsync("admin@email.com").Result;
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@email.com"
                };
                var result = userMgr.CreateAsync(admin, "Pa5$w0rd").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                result = userMgr.AddClaimsAsync(admin, new Claim[]{
                    new Claim(JwtClaimTypes.Name, "Super Admin"),
                    new Claim(JwtClaimTypes.GivenName, "Super"),
                    new Claim(JwtClaimTypes.FamilyName, "Admin"),
                    new Claim(JwtClaimTypes.Email, "admin@email.com"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean)
                }).Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
            }
        }

        private static void InitializeConfigurationDbContext(IConfiguration seedDataConfig, IServiceScope serviceScope)
        {
            serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();

            if (seedDataConfig == null)
            {
                return;
            }

            if (!context.Clients.Any())
            {
                foreach (var client in InMemoryConfig.GetClients(seedDataConfig.GetSection("Clients")))
                {
                    context.Clients.Add(client.ToEntity());
                }

                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in InMemoryConfig.GetIds(seedDataConfig.GetSection("Ids")))
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }

                context.SaveChanges();
            }

            if (!context.ApiResources.Any())
            {
                foreach (var resource in InMemoryConfig.GetApis(seedDataConfig.GetSection("Apis")))
                {
                    context.ApiResources.Add(resource.ToEntity());
                }

                foreach (var resource in InMemoryConfig.GetApiScopes(seedDataConfig.GetSection("Apis")))
                {
                    context.ApiScopes.Add(resource.ToEntity());
                }

                context.SaveChanges();
            }
        }
    }
}
