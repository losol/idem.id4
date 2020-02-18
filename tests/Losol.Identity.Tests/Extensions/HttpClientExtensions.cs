using IdentityModel;
using IdentityModel.Client;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Losol.Identity.Tests.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> SignInRedirectAsync(this HttpClient client)
        {
            var discoveryDocument = await client.GetDiscoveryDocumentAsync();
            Assert.False(discoveryDocument.IsError);

            var codeVerifier = CryptoRandom.CreateUniqueId(32);

            // create code_challenge
            string codeChallenge;
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                codeChallenge = Base64Url.Encode(challengeBytes);
            }

            var response = await client.GetAsync(discoveryDocument.AuthorizeEndpoint, new Dictionary<string, string>
            {
                { OidcConstants.AuthorizeRequest.ClientId, ClientId },
                { OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code },
                { OidcConstants.AuthorizeRequest.RedirectUri, "http://integration-tests.local/callback.html" },
                { OidcConstants.AuthorizeRequest.CodeChallenge, codeChallenge },
                { OidcConstants.AuthorizeRequest.CodeChallengeMethod, "S256" },
                { OidcConstants.AuthorizeRequest.Scope, "openid profile demo.api" }
            });

            await response.CheckIsSuccessfulAsync();
            return response;
        }

        public static async Task<HttpResponseMessage> SendPhoneVerificationTokenAsync(this HttpClient client, string phoneNumber)
        {
            var authResponse = await client.SignInRedirectAsync();
            var antiForgeryToken = await authResponse.GetAntiForgeryTokenAsync();
            var response = await client.PostAsync(authResponse.RequestMessage.RequestUri.ToString(), new Dictionary<string, string>
            {
                { "button", "sendSMS" },
                { "PhoneNumber", phoneNumber },
                { HttpResponseMessageExtensions.RequestVerificationTokenParam, antiForgeryToken }
            });
            await response.CheckIsSuccessfulAsync();
            return response;
        }

        public static async Task<HttpResponseMessage> GetAsync(
            this HttpClient client,
            string uri,
            IDictionary<string, string> queryParams)
        {
            var query = queryParams
                .Select(e => $"{Uri.EscapeDataString(e.Key)}={Uri.EscapeDataString(e.Value)}")
                .Aggregate((v1, v2) => $"{v1}&{v2}");

            var fullUri = $"{uri}?{query}";
            var response = await client.GetAsync(fullUri);
            HandleAntiForgeryCookie(client, response);
            return response;
        }

        public static async Task<HttpResponseMessage> PostAsync(
            this HttpClient client,
            string uri,
            IDictionary<string, string> formData)
        {
            var response = await client.PostAsync(uri, new FormUrlEncodedContent(formData
                .Select(e => KeyValuePair.Create(e.Key, e.Value))));
            HandleAntiForgeryCookie(client, response);
            return response;
        }

        private static void HandleAntiForgeryCookie(HttpClient client, HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var antiForgeryCookie = response.GetAntiForgeryCookie();
                if (antiForgeryCookie != null)
                {
                    client.DefaultRequestHeaders.Add("Cookie",
                        new CookieHeaderValue(antiForgeryCookie.Name, antiForgeryCookie.Value).ToString());
                }
            }
        }

        private const string ClientId = "test";
    }
}
