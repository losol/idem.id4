using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Losol.Communication.Sms;
using Losol.Identity.Data;
using Losol.Identity.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Losol.Identity.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        public readonly Mock<ISmsSender> SmsSenderMock = new Mock<ISmsSender>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseSolutionRelativeContentRoot("src/Losol.Identity")
                .UseEnvironment("Development")
                .ConfigureAppConfiguration(app => app
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "IdentityServer:ConfigurationType", "InMemory" },
                        { "SkipDbInitialization", bool.TrueString }, // don't run migrations on in-memory DB
                        { "InMemoryConfiguration:Ids:0", "OpenId" },
                        { "InMemoryConfiguration:Ids:1", "Profile" },
                        { "InMemoryConfiguration:Ids:2", "Phone" },
                        { "InMemoryConfiguration:Apis:0:Id", "demo.api" },
                        { "InMemoryConfiguration:Apis:0:Name", "Demo API" },
                        { "InMemoryConfiguration:Apis:0:UserClaims:0", "role" },
                        { "InMemoryConfiguration:Apis:0:UserClaims:1", "phone_number" },
                        { "InMemoryConfiguration:Clients:0:Id", "test" },
                        { "InMemoryConfiguration:Clients:0:Name", "Integration Tests Client" },
                        { "InMemoryConfiguration:Clients:0:Url", "http://integration-tests.local" },
                        { "InMemoryConfiguration:Clients:0:UserClaims:0", "role" },
                        { "InMemoryConfiguration:Clients:0:UserClaims:1", "phone_number" },
                        { "InMemoryConfiguration:Clients:0:RedirectPaths:0", "/callback.html" },
                        { "InMemoryConfiguration:Clients:0:PostLogoutRedirectPaths:0", "/index.html" },
                        { "InMemoryConfiguration:Clients:0:AllowedScopes:0", "openid" },
                        { "InMemoryConfiguration:Clients:0:AllowedScopes:1", "profile" },
                        { "InMemoryConfiguration:Clients:0:AllowedScopes:2", "demo.api" },
                        { "InMemoryConfiguration:Clients:0:AllowedGrantTypes:0", "authorization_code" },
                        { "InMemoryConfiguration:Clients:0:AllowedCorsOrigins:0", "http://integration-tests.local" },
                        { "InMemoryConfiguration:Clients:0:RequirePkce", bool.TrueString },
                        { "InMemoryConfiguration:Clients:0:RequireConsent", bool.FalseString },
                        { "InMemoryConfiguration:Clients:0:RequireClientSecret", bool.FalseString },
                        { "InMemoryConfiguration:Clients:0:AllowAccessTokensViaBrowser", bool.TrueString },
                        { "InMemoryConfiguration:Clients:0:Properties:EnablePasswordLogin", bool.TrueString },
                        { "InMemoryConfiguration:Clients:0:Properties:EnablePhoneLogin", bool.TrueString }
                    }))
                .ConfigureServices(services =>
                {
                    // Override already added email sender with the true mock
                    services.AddSingleton(SmsSenderMock.Object);

                    // Remove the app's ApplicationDbContext registration.
                    var descriptor = services.SingleOrDefault(
                    d => d.ServiceType ==
                         typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing.
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("idem-itests");
                    });
                });
        }

        public async Task CleanupAsync()
        {
            SmsSenderMock.Reset();

            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var users = userManager.Users.ToArray();
            await Task.WhenAll(users.Select(u => (Task)userManager.DeleteAsync(u)).ToArray());
        }
    }
}
