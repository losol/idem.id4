using IdentityModel.Client;
using Losol.Identity.Tests.Extensions;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Losol.Identity.Tests.Controllers
{
    public class PhoneNumberVerificationControllerTests : IClassFixture<CustomWebApplicationFactory<Startup>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;

        public PhoneNumberVerificationControllerTests(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        public void Dispose()
        {
            _factory.CleanupAsync().Wait();
        }

        [Theory]
        [MemberData(nameof(InvalidPhoneNumbers))]
        public async Task Should_Require_Valid_Phone_Number_To_Send_Verification_Code(string phone)
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Should_Send_Sms_With_Phone_Verification_Code_To_Non_Existing_User()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone = ValidPhoneNumber
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _factory.SmsSenderMock.Verify(s => s
                .SendSmsAsync(ValidPhoneNumber, It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [MemberData(nameof(InvalidPhoneNumbers))]
        public async Task Should_Not_Resend_Phone_Verification_Code_To_Invalid_Phone_Number(string invalidPhoneNumber)
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone = ValidPhoneNumber
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _factory.SmsSenderMock.Verify(s => s
                .SendSmsAsync(ValidPhoneNumber, It.IsAny<string>()), Times.Once);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            var resendToken = json.Value<string>("resend_token");
            Assert.NotEmpty(resendToken);

            _factory.SmsSenderMock.Reset();

            response = await client.PutAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone = invalidPhoneNumber,
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            _factory.SmsSenderMock.Verify(s => s.SendSmsAsync(It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Should_Not_Resend_Phone_Verification_Code_If_Reset_Token_Is_Wrong()
        {
            var client = _factory.CreateClient();
            var response = await client.PutAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone = ValidPhoneNumber,
                    resend_token = "some-token"
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            _factory.SmsSenderMock.Verify(s => s.SendSmsAsync(It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Should_Allow_To_Resend_Phone_Verification_Code_To_Non_Existing_User()
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone = ValidPhoneNumber
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _factory.SmsSenderMock.Verify(s => s
                .SendSmsAsync(ValidPhoneNumber, It.IsAny<string>()), Times.Once);

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            var resendToken = json.Value<string>("resend_token");
            Assert.NotEmpty(resendToken);

            _factory.SmsSenderMock.Reset();

            response = await client.PutAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone = ValidPhoneNumber,
                    resend_token = resendToken
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _factory.SmsSenderMock.Verify(s => s
                .SendSmsAsync(ValidPhoneNumber, It.IsAny<string>()), Times.Once);

            json = JToken.Parse(await response.Content.ReadAsStringAsync());
            var resendToken2 = json.Value<string>("resend_token");
            Assert.NotEmpty(resendToken);
            Assert.NotEqual(resendToken, resendToken2);
        }

        [Fact]
        public async Task Should_Generate_Valid_Verification_Code()
        {
            var client = _factory.CreateClient();

            var message = "";
            _factory.SmsSenderMock.Setup(s => s
                    .SendSmsAsync(ValidPhoneNumber, It.IsAny<string>()))
                .Callback<string, string>((phoneNumber, text) =>
                {
                    message = text;
                });

            var response = await client.PostAsync("/api/phone/verification",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    phone = ValidPhoneNumber
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotEmpty(message);

            _factory.SmsSenderMock.Verify(s => s
                .SendSmsAsync(ValidPhoneNumber, message), Times.Once);

            var m = VerificationTokenPattern.Match(message);
            Assert.True(m.Success);

            var verificationCode = m.Groups[1];
            Assert.NotNull(verificationCode);

            var accessToken = await client
                .ExchangePhoneVerificationCodeWithAccessTokenAsync(
                    ValidPhoneNumber, verificationCode.ToString());

            Assert.NotEmpty(accessToken);
        }

        private const string ValidPhoneNumber = "+11111111111";

        public static IEnumerable<object[]> InvalidPhoneNumbers => new List<object[]>{
            new object[]{null},
            new object[]{""},
            new object[]{"   "},
            new object[]{"asd"}
        };

        private static readonly Regex VerificationTokenPattern = new Regex("(\\d+)");
    }
}
