using System.ComponentModel.DataAnnotations;

namespace Losol.Identity.Controllers.Account
{
    public class SmsCodeVerificationModel
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string SmsCode { get; set; }

        [Required]
        public string ReturnUrl { get; set; }
    }
}
