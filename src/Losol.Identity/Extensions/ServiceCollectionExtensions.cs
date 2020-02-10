using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Losol.Identity.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureLocalization(this IServiceCollection services, CultureInfo defaultCultureInfo)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            CultureInfo.DefaultThreadCurrentCulture = defaultCultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = defaultCultureInfo;
        }
    }
}
