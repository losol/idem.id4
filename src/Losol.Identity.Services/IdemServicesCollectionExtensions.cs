using Losol.Identity.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Losol.Identity.Services
{
    public static class IdemServicesCollectionExtensions
    {
        public static IServiceCollection AddIdemServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.TryAddTransient<IPhoneAuthenticationService, PhoneAuthenticationService>();
            return services;
        }
    }
}
