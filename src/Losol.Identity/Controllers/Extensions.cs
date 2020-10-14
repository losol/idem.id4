using IdentityServer4.Models;
using Losol.Identity.Controllers.Account;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using IdentityServer4.Stores;

namespace Losol.Identity.Controllers
{
    public static class Extensions
    {
        public static async Task<bool> IsPkceClientAsync(this IClientStore store, string client_id)
        {
            if (string.IsNullOrWhiteSpace(client_id))
            {
                return false;
            }
            var client = await store.FindEnabledClientByIdAsync(client_id);
            return client?.RequirePkce == true;
        }

        /// <summary>
        /// Checks if the redirect URI is for a native client.
        /// </summary>
        /// <returns></returns>
        public static bool IsNativeClient(this AuthorizationRequest context)
        {
            return !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
                   && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
        }

        public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri)
        {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = "";

            return controller.View(viewName, new RedirectViewModel { RedirectUrl = redirectUri });
        }
    }
}
