using IdentityModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Losol.Identity
{
    public static class IdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddInMemoryConfiguration(this IIdentityServerBuilder builder, IConfigurationSection config)
        {
            return builder
                .AddInMemoryIdentityResources(Config.GetIds(config.GetSection("Ids")))
                .AddInMemoryApiResources(Config.GetApis(config.GetSection("Apis")))
                .AddInMemoryClients(Config.GetClients(config.GetSection("Clients")));
        }

        public static IIdentityServerBuilder AddDatabaseConfiguration(this IIdentityServerBuilder builder,
            IConfigurationSection config,
            string connectionString)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            return builder.AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseNpgsql(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseNpgsql(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));

                    var c = config.GetSection("OperationalStore").Get<OperationalStoreConfig>();
                    if (c != null)
                    {
                        options.EnableTokenCleanup = c.EnableTokenCleanup;
                        options.TokenCleanupInterval = c.TokenCleanupInterval;
                    }
                });
        }

        public static IIdentityServerBuilder AddSigningCredentialFromLocalMachineStorage(this IIdentityServerBuilder builder, string commonName)
        {
            //The one that expires last at the top
            var certs = X509.LocalMachine.My.SubjectDistinguishedName.Find("CN=" + commonName, false)
                .Where(o => DateTime.UtcNow >= o.NotBefore)
                .OrderByDescending(o => o.NotAfter)
                .ToArray();

            if (!certs.Any())
            {
                throw new Exception("No valid certificates could be found.");
            }

            //Get first (in desc order of expiry) th
            var signingCert = certs.FirstOrDefault();
            if (signingCert == null)
            {
                throw new InvalidOperationException("No valid signing certificate could be found.");
            }

            var signingCredential = new SigningCredentials(new X509SecurityKey(signingCert), "RS256");
            builder.AddSigningCredential(signingCredential);

            foreach (var cert in certs)
            {
                builder.AddValidationKey(cert);
            }

            return builder;
        }
    }
}
