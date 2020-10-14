using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Losol.Identity.Tests.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task CheckIsSuccessfulAsync(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new XunitException(await response.Content.ReadAsStringAsync());
            }
        }

        public static SetCookieHeaderValue GetAntiForgeryCookie(this HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                var setCookieHeaderValues = SetCookieHeaderValue.ParseList(values.ToList());
                return setCookieHeaderValues.SingleOrDefault(c =>
                    c.Name.StartsWith(".AspNetCore.AntiForgery.", StringComparison.InvariantCultureIgnoreCase));
            }

            return null;
        }

        public static async Task<string> GetAntiForgeryTokenAsync(this HttpResponseMessage response)
        {
            return await response.GetHiddenInputValueAsync(RequestVerificationTokenParam);
        }

        public static async Task<string> GetHiddenInputValueAsync(this HttpResponseMessage response, string inputName)
        {
            var responseHtml = await response.Content.ReadAsStringAsync();
            var matches = HiddenFieldsRegex.Matches(responseHtml);
            foreach (var m in matches)
            {
                var str = m.ToString();
                var match = new Regex($@"name=""{inputName}"".*value=""([^""]+)""").Match(str);
                if (match.Success)
                {
                    var value = match.Groups[1].Captures[0].Value;
                    return WebUtility.HtmlDecode(value);
                }
            }
            return null;
        }

        public static async Task<string> GetFormActionUrlAsync(this HttpResponseMessage response)
        {
            var responseHtml = await response.Content.ReadAsStringAsync();
            var match = new Regex(@"\<form action=""([^""]+)""").Match(responseHtml);
            var url = match.Success ? WebUtility.HtmlDecode(match.Groups[1].Captures[0].Value) : null;
            return url ?? response.RequestMessage.RequestUri.ToString();
        }

        public const string RequestVerificationTokenParam = "__RequestVerificationToken";

        private static readonly Regex HiddenFieldsRegex = new Regex(@"\<(input.*?type=""hidden"".*?)\/\>");
    }
}
