using System;
using System.Threading.Tasks;
using Losol.Identity.Model;

namespace Losol.Identity.Services.Auth
{
    public interface IPhoneAuthenticationService
    {
        Task<ApplicationUser> GetUserByPhoneAsync(string phoneNumber);

        Task<ApplicationUser> SendVerificationCodeAsync(string key, string phoneNumber);

        Task<ApplicationUser> AuthenticateAsync(
            string key,
            string phoneNumber,
            string verificationToken,
            bool createUserIfNotExists = false);
    }
}
