using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Losol.Identity.Model;
using Losol.Identity.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using static Losol.Identity.Constants.AuthConstants;
using static Losol.Identity.Constants.AuthConstants.GrantType;
using static Losol.Identity.Constants.AuthConstants.TokenPurpose;
using static Losol.Identity.Constants.AuthConstants.TokenRequest;

namespace Losol.Identity.Validation
{
    public class PhoneNumberTokenGrantValidator : IExtensionGrantValidator
    {
        private readonly PhoneNumberTokenProvider<ApplicationUser> _phoneNumberTokenProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEventService _events;
        private readonly ILogger<PhoneNumberTokenGrantValidator> _logger;

        public PhoneNumberTokenGrantValidator(
            PhoneNumberTokenProvider<ApplicationUser> phoneNumberTokenProvider,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEventService events,
            ILogger<PhoneNumberTokenGrantValidator> logger)
        {
            _phoneNumberTokenProvider = phoneNumberTokenProvider;
            _userManager = userManager;
            _signInManager = signInManager;
            _events = events;
            _logger = logger;
        }

        public string GrantType => PhoneNumberToken;

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var createUser = false;
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

            var user = await _userManager.Users.SingleOrDefaultAsync(x =>
                x.PhoneNumber == PhoneNumberUtil.NormalizePhoneNumber(phoneNumber));
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = phoneNumber,
                    PhoneNumber = phoneNumber,
                    SecurityStamp = phoneNumber.Sha256()
                };
                createUser = true;
            }

            var valid = await _phoneNumberTokenProvider
                .ValidateAsync(PhoneNumberVerificationPurpose, verificationToken, _userManager, user);

            if (!valid)
            {
                _logger.LogInformation("Authentication failed for token: {token}, reason: invalid token",
                    verificationToken);
                await _events.RaiseAsync(new UserLoginFailureEvent(verificationToken,
                    "invalid token or verification id", false));
                return;
            }

            if (createUser)
            {
                user.PhoneNumberConfirmed = true;
                var result = await _userManager.CreateAsync(user);
                if (result != IdentityResult.Success)
                {

                    var reason = result.Errors.Select(x => x.Description)
                        .Aggregate((a, b) => a + ", " + b);
                    _logger.LogInformation("User creation failed: {username}, reason: {reason}",
                        phoneNumber, reason);
                    await _events.RaiseAsync(new UserLoginFailureEvent(phoneNumber,
                        reason, false));
                    return;
                }
            }

            _logger.LogInformation("Credentials validated for username: {phoneNumber}", phoneNumber);
            await _events.RaiseAsync(new UserLoginSuccessEvent(phoneNumber, user.Id, phoneNumber, false));
            await _signInManager.SignInAsync(user, true);
            context.Result = new GrantValidationResult(user.Id, OidcConstants.AuthenticationMethods.ConfirmationBySms);
        }
    }
}
