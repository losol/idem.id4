using IdentityServer4.Models;
using Losol.Communication.Sms;
using Losol.Identity.Constants;
using Losol.Identity.Model;
using Losol.Identity.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Losol.Identity.Controllers
{
    [Route("api/phone/verification")]
    public class PhoneNumberVerificationController : ControllerBase
    {
        private readonly ISmsSender _smsService;
        private readonly DataProtectorTokenProvider<ApplicationUser> _dataProtectorTokenProvider;
        private readonly PhoneNumberTokenProvider<ApplicationUser> _phoneNumberTokenProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PhoneNumberVerificationController> _logger;

        public PhoneNumberVerificationController(
            ISmsSender smsService,
            DataProtectorTokenProvider<ApplicationUser> dataProtectorTokenProvider,
            PhoneNumberTokenProvider<ApplicationUser> phoneNumberTokenProvider,
            UserManager<ApplicationUser> userManager,
            ILogger<PhoneNumberVerificationController> logger)
        {
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            _dataProtectorTokenProvider = dataProtectorTokenProvider ?? throw new ArgumentNullException(nameof(dataProtectorTokenProvider));
            _phoneNumberTokenProvider = phoneNumberTokenProvider ?? throw new ArgumentNullException(nameof(phoneNumberTokenProvider));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var user = await GetUserByPhoneAsync(model.PhoneNumber);
                var verifyToken = await _phoneNumberTokenProvider
                    .GenerateAsync(AuthConstants.TokenPurpose.PhoneNumberVerificationPurpose, _userManager, user);

                await SendVerificationCodeAsync(model.PhoneNumber, verifyToken);

                var resendToken = await _dataProtectorTokenProvider
                    .GenerateAsync(AuthConstants.TokenPurpose.ResendTokenPurpose, _userManager, user);

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

            var user = await GetUserByPhoneAsync(model.PhoneNumber);
            if (!await _dataProtectorTokenProvider.ValidateAsync(AuthConstants.TokenPurpose.ResendTokenPurpose,
                model.ResendToken, _userManager, user))
            {
                return BadRequest("Invalid resend token");
            }

            try
            {
                var verifyToken = await _phoneNumberTokenProvider
                    .GenerateAsync(AuthConstants.TokenPurpose.PhoneNumberVerificationPurpose, _userManager, user);

                await SendVerificationCodeAsync(model.PhoneNumber, verifyToken);

                var newResendToken = await _dataProtectorTokenProvider
                    .GenerateAsync(AuthConstants.TokenPurpose.ResendTokenPurpose, _userManager, user);

                return Ok(new PhoneVerificationResponseModel(newResendToken));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to resend verification code to {phoneNumber}", model.PhoneNumber);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<ApplicationUser> GetUserByPhoneAsync(string phoneNumber)
        {
            phoneNumber = PhoneNumberUtil.NormalizePhoneNumber(phoneNumber);
            return await _userManager.Users.SingleOrDefaultAsync(x => x.PhoneNumber == phoneNumber)
                   ?? new ApplicationUser
                   {
                       Id = "dummy-user-id",
                       PhoneNumber = phoneNumber,
                       SecurityStamp = phoneNumber.Sha256()
                   };
        }

        private async Task SendVerificationCodeAsync(string phoneNumber, string verificationCode)
        {
            // TODO: use message queue for this
            // TODO: localize
            await _smsService.SendSmsAsync(phoneNumber, $"Your login verification code is: {verificationCode}");
        }
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
