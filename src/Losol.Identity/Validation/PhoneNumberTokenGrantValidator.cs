using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Losol.Identity.Services.Auth;
using System.Threading.Tasks;
using static Losol.Identity.Constants.AuthConstants;
using static Losol.Identity.Constants.AuthConstants.GrantType;
using static Losol.Identity.Constants.AuthConstants.TokenRequest;

namespace Losol.Identity.Validation
{
    public class PhoneNumberTokenGrantValidator : IExtensionGrantValidator
    {
        private readonly IPhoneAuthenticationService _phoneAuthenticationService;

        public PhoneNumberTokenGrantValidator(IPhoneAuthenticationService phoneAuthenticationService)
        {
            _phoneAuthenticationService = phoneAuthenticationService;
        }

        public string GrantType => PhoneNumberToken;

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var raw = context.Request.Raw;
            var credential = raw.Get(OidcConstants.TokenRequest.GrantType);
            if (credential != PhoneNumberToken)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant,
                    "invalid verify_phone_number_token credential");
                return;
            }

            var phoneNumber = raw.Get(PhoneNumber);
            var verificationToken = raw.Get(TokenRequest.Token);

            var user = await _phoneAuthenticationService.AuthenticateAsync(phoneNumber, verificationToken, true);
            if (user != null)
            {
                context.Result = new GrantValidationResult(user.Id, OidcConstants.AuthenticationMethods.ConfirmationBySms);
            }
        }
    }
}
