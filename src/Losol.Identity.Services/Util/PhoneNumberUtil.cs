using System.Text.RegularExpressions;

namespace Losol.Identity.Services.Util
{
    public static class PhoneNumberUtil
    {
        private static readonly Regex PhoneNumber = new Regex("[\\(\\)\\-\\s0-9\\+]+");
        private static readonly Regex NonDigit = new Regex("[^\\+0-9]");

        public static string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return phoneNumber;
            }
            var number = phoneNumber.Trim();
            return NonDigit.Replace(number, "");
        }

        public static bool IsPhoneNumber(string value)
        {
            return PhoneNumber.IsMatch(value);
        }
    }
}
