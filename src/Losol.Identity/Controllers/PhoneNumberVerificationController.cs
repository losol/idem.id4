using Losol.Communication.Sms;
using Losol.Identity.Model;
using Losol.Identity.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Losol.Identity.Controllers
{
    [Route(Path)]
    public class PhoneNumberVerificationController : ControllerBase
    {
        public const string Path = "/api/phone/verification";

        private readonly ISmsSender _smsService;
        private readonly DataProtectorTokenProvider<ApplicationUser> _dataProtectorTokenProvider;
        private readonly PhoneNumberTokenProvider<ApplicationUser> _phoneNumberTokenProvider;
        private readonly IPhoneAuthenticationService _phoneAuthenticationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PhoneNumberVerificationController> _logger;

        public PhoneNumberVerificationController(
            ISmsSender smsService,
            DataProtectorTokenProvider<ApplicationUser> dataProtectorTokenProvider,
            PhoneNumberTokenProvider<ApplicationUser> phoneNumberTokenProvider,
            UserManager<ApplicationUser> userManager,
            ILogger<PhoneNumberVerificationController> logger,
            IPhoneAuthenticationService phoneAuthenticationService)
        {
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            _dataProtectorTokenProvider = dataProtectorTokenProvider ?? throw new ArgumentNullException(nameof(dataProtectorTokenProvider));
            _phoneNumberTokenProvider = phoneNumberTokenProvider ?? throw new ArgumentNullException(nameof(phoneNumberTokenProvider));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _phoneAuthenticationService = phoneAuthenticationService;
        }

        [HttpPost]
        public async Task<IActionResult> SendVerificationToken([FromBody] PhoneVerificationRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: check Captcha

            try
            {
                var user = await _phoneAuthenticationService.SendVerificationCodeAsync(model.PhoneNumber);

                var resendToken = await _dataProtectorTokenProvider
                    .GenerateAsync(ResendTokenPurpose, _userManager, user);

                return Ok(new PhoneVerificationResponseModel(resendToken));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send verification code to {phoneNumber}", model.PhoneNumber);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut]
        public async Task<IActionResult> ResendVerificationToken([FromBody] PhoneVerificationRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(model.ResendToken))
            {
                return BadRequest("resend_token is required");
            }

            // TODO: check Captcha

            var user = await _phoneAuthenticationService.GetUserByPhoneAsync(model.PhoneNumber);
            if (!await _dataProtectorTokenProvider.ValidateAsync(ResendTokenPurpose,
                model.ResendToken, _userManager, user))
            {
                return BadRequest("Invalid resend token");
            }

            try
            {
                user = await _phoneAuthenticationService.SendVerificationCodeAsync(model.PhoneNumber);

                var newResendToken = await _dataProtectorTokenProvider
                    .GenerateAsync(ResendTokenPurpose, _userManager, user);

                return Ok(new PhoneVerificationResponseModel(newResendToken));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to resend verification code to {phoneNumber}", model.PhoneNumber);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        public const string ResendTokenPurpose = "resend_token";
    }

    public class PhoneVerificationRequestModel
    {
        [Required]
        [Phone]
        [JsonPropertyName("phone")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("resend_token")]
        public string ResendToken { get; set; }
    }

    public class PhoneVerificationResponseModel
    {
        [JsonPropertyName("resend_token")]
        public string ResendToken { get; }

        public PhoneVerificationResponseModel(string resendToken)
        {
            ResendToken = resendToken;
        }
    }
}
