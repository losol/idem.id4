using IdentityServer4.Models;
using Losol.Identity.Util;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Losol.Identity.Config
{
    public class InMemoryConfig
    {
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
                .Select(s => new ApiResource(s["Id"], s["Name"])
                {
                    UserClaims = s.GetSection("UserClaims")
                        .GetChildren()
                        .Select(c => c.Value)
                        .ToHashSet()
                })
                .ToArray();
        }

        public static IEnumerable<Client> GetClients(IConfigurationSection configuration)
        {
            return configuration
                .GetChildren()
                .Select(c =>
                {
                    // TODO: check required config options

                    var baseUrl = c["Url"];

                    var client = new Client
                    {
                        ClientId = c["Id"],
                        ClientName = c["Name"],
                        AllowOfflineAccess = bool.TrueString.Equals(c["AllowOfflineAccess"]),
                        AllowedScopes = c.GetSection("AllowedScopes")
                            .GetChildren()
                            .Select(s => s.Value)
                            .ToArray(),
                        AllowedGrantTypes = c.GetSection("AllowedGrantTypes")
                            .GetChildren()
                            .Select(s => s.Value)
                            .ToArray(),
                        RedirectUris = c.GetSection("RedirectPaths")
                            .GetChildren()
                            .Select(s => UriUtil.BuildUri(s.Value, baseUrl))
                            .ToArray(),
                        PostLogoutRedirectUris = c.GetSection("PostLogoutRedirectPaths")
                            .GetChildren()
                            .Select(s => UriUtil.BuildUri(s.Value, baseUrl))
                            .ToArray(),
                    };

                    var clientSecret = c["Secret"];
                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        client.ClientSecrets = new[] { new Secret(clientSecret.Sha256()) };
                    }

                    var frontChannelLogoutPath = c["FrontChannelLogoutPath"];
                    if (!string.IsNullOrEmpty(frontChannelLogoutPath))
                    {
                        client.FrontChannelLogoutUri = UriUtil.BuildUri(frontChannelLogoutPath, baseUrl);
                    }

                    if (client.AllowedGrantTypes.Contains(GrantType.AuthorizationCode))
                    {
                        client.RequirePkce = true;
                        client.RequireClientSecret = false;
                        client.AllowAccessTokensViaBrowser = true;
                        client.AllowedCorsOrigins = c.GetSection("CorsOrigins")?
                                                        .GetChildren()
                                                        .Select(s => UriUtil.BuildUri(s.Value, baseUrl))
                                                        .ToArray() ?? new[] { baseUrl };
                    }

                    return client;
                })
                .ToArray();
        }
    }
}
