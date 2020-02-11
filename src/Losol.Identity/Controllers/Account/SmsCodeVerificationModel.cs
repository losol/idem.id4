using System.ComponentModel.DataAnnotations;

namespace Losol.Identity.Controllers.Account
{
    public class SmsCodeVerificationModel
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string TokenKey { get; set; }

        public string SmsCode { get; set; }

        [Required]
        public string ReturnUrl { get; set; }
    }
}
