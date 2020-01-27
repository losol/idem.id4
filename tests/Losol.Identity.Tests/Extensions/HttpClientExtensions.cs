using IdentityModel.Client;
using Losol.Identity.Constants;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Losol.Identity.Tests.Extensions
{
    public static class HttpClientExtensions
    {
        public static string TestClientId = "test";
        public static string TestClientSecret = "secret";

        public static async Task<string> ExchangePhoneVerificationCodeWithAccessTokenAsync(
            this HttpClient client,
            string phoneNumber,
            string verificationCode)
        {
            var discoveryDocument = await client.GetDiscoveryDocumentAsync();
            Assert.False(discoveryDocument.IsError);

            var tokenRequest = new TokenRequest
            {
                GrantType = AuthConstants.GrantType.PhoneNumberToken,
                ClientId = TestClientId,
                ClientSecret = TestClientSecret,
                Address = discoveryDocument.TokenEndpoint,
                Parameters = new Dictionary<string, string>
                {
                    {"phone_number", phoneNumber},
                    {"verification_token", verificationCode}
                }
            };

            var tokenResponse = await client.RequestTokenAsync(tokenRequest);
            Assert.False(tokenResponse.IsError);
            return tokenResponse.AccessToken;
        }
    }
}
