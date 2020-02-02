// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Modifications copyright (C) 2020 Losol AS

using System.ComponentModel.DataAnnotations;

namespace Losol.Identity.Controllers.Account
{
    [LoginInput]
    public class LoginInputModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public bool RememberLogin { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class LoginInputAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value,
            ValidationContext validationContext)
        {
            var model = (LoginInputModel)validationContext.ObjectInstance;
            if ((string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password)) &&
                string.IsNullOrEmpty(model.PhoneNumber))
            {
                return new ValidationResult("No login info provided");
            }

            return ValidationResult.Success;
        }
    }
}
