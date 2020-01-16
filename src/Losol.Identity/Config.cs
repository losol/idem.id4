using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;

namespace Losol.Identity
{
    public class Config
    {
        public enum AuthFlow
        {
            Hybrid,
            Pkce
        }

        public static IEnumerable<IdentityResource> GetIds(IConfiguration configuration)
        {
            return configuration
                .GetChildren()
                .Select(s =>
                    {
                        var assemblyName = typeof(IdentityResources).Assembly.FullName;
                        var objectToInstantiate = $"{typeof(IdentityResources).FullName}+{s.Value}, {assemblyName}";
                        var type = Type.GetType(objectToInstantiate);
                        if (type == null)
                        {
                            throw new ArgumentException($"Cannot find identity resource {s.Value}");
                        }
                        return (IdentityResource)Activator.CreateInstance(type);
                    })
                .ToArray();
        }

        public static IEnumerable<ApiResource> GetApis(IConfigurationSection configuration)
        {
            return configuration
                .GetChildren()
                .Select(s => new ApiResource(s["Id"], s["Name"]))
                .ToArray();
        }

        public static IEnumerable<Client> GetClients(IConfigurationSection configuration)
        {
            return configuration
                .GetChildren()
                .Select(c =>
                {
                    var baseUrl = c["Url"].TrimEnd('/');

                    var scopes = c.GetSection("AllowedScopes")
                        .GetChildren()
                        .Select(c => c.Value)
                        .ToArray();

                    if (!Enum.TryParse<AuthFlow>(c["Flow"], out var flow))
                    {
                        throw new ArgumentException($"Invalid auth flow: ${flow}");
                    }

                    var client = new Client
                    {
                        ClientId = c["Id"],
                        ClientName = c["Name"],
                        RequireConsent = false,
                        RedirectUris = c.GetSection("RedirectPaths")
                            .GetChildren()
                            .Select(s => $"{baseUrl}/{s.Value.TrimStart('/')}")
                            .ToArray(),
                        PostLogoutRedirectUris = c.GetSection("PostLogoutRedirectPaths")
                            .GetChildren()
                            .Select(s => $"{baseUrl}/{s.Value.TrimStart('/')}")
                            .ToArray(),
                        AllowedScopes = scopes
                    };

                    switch (flow)
                    {
                        case AuthFlow.Hybrid:
                            client.ClientSecrets = new[] { new Secret(c["Secret"].Sha256()) };
                            client.AllowedGrantTypes = GrantTypes.HybridAndClientCredentials;
                            client.FrontChannelLogoutUri =
                                $"{baseUrl}/{c["FrontChannelLogoutPath"]?.TrimStart('/')}";
                            client.AllowOfflineAccess = true;
                            break;

                        case AuthFlow.Pkce:
                            client.AllowedGrantTypes = GrantTypes.Code;
                            client.RequirePkce = true;
                            client.RequireClientSecret = false;
                            client.AllowAccessTokensViaBrowser = true;
                            client.AllowedCorsOrigins = c.GetSection("CorsOrigins")?
                                                            .GetChildren()
                                                            .Select(s => $"{baseUrl}/{s.Value.TrimStart('/')}")
                                                            .ToArray() ?? new[] { baseUrl };
                            break;
                    }

                    return client;
                })
                .ToArray();
        }
    }
}
