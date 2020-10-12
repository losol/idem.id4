using Losol.Identity.Services.Util;
using Xunit;

namespace Losol.Identity.Services.Tests.Util
{
    public class PhoneNumberUtilTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("   ", "")]
        [InlineData("1", "1")]
        [InlineData("+1111", "+1111")]
        [InlineData("+1(111)1111", "+11111111")]
        [InlineData("+1 - 111 - 1111", "+11111111")]
        [InlineData("+1 (111) 11-11", "+11111111")]
        public void Should_Normalize_Phone_Numbers(string phoneNumber, string expectedResult)
        {
            Assert.Equal(expectedResult, PhoneNumberUtil.NormalizePhoneNumber(phoneNumber));
        }
    }
}
