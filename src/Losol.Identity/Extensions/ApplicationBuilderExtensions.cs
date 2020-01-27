using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Losol.Identity.Config;
using Losol.Identity.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Losol.Identity.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void InitializeDatabase(this IApplicationBuilder app, ConfigurationType configurationType, IConfiguration seedDataConfig = null)
        {
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

            if (configurationType == ConfigurationType.InMemory)
            {
                return;
            }

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
                context.SaveChanges();
            }
        }
    }
}
