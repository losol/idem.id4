using System.Text.RegularExpressions;

namespace Losol.Identity.Util
{
    public static class PhoneNumberUtil
    {
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
    }
}
