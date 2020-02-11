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
                        { "SkipDbInitialization", bool.TrueString }
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
