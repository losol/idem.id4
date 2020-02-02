using System.Threading.Tasks;
using Losol.Identity.Model;

namespace Losol.Identity.Services.Auth
{
    public interface IPhoneAuthenticationService
    {
        Task<ApplicationUser> GetUserByPhoneAsync(string phoneNumber);

        Task<ApplicationUser> SendVerificationCodeAsync(string phoneNumber);

        Task<ApplicationUser> AuthenticateAsync(
            string phoneNumber,
            string verificationToken,
            bool createUserIfNotExists = false);
    }
}
