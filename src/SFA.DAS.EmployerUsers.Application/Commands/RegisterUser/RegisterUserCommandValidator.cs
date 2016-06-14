﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SFA.DAS.EmployerUsers.Application.Validation;

namespace SFA.DAS.EmployerUsers.Application.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : IValidator<RegisterUserCommand>
    {
        public ValidationResult Validate(RegisterUserCommand item)
        {
            var validationResult = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(item.Email))
            {
                validationResult.AddError(nameof(item.Email), "Please enter email address");
            }

            if (string.IsNullOrEmpty(item.FirstName))
            {
                validationResult.AddError(nameof(item.FirstName), "Please enter first name");
            }

            if (string.IsNullOrEmpty(item.LastName))
            {
                validationResult.AddError(nameof(item.LastName), "Please enter last name");
            }

            if (string.IsNullOrEmpty(item.Password))
            {
                validationResult.AddError(nameof(item.Password), "Please enter password");
            }
            else if (CheckPasswordMatchesAtLeastOneUppercaseOneLowercaseOneNumberAndAtLeastEightCharacters(item.Password))
            {
                validationResult.AddError(nameof(item.Password), "Password requires upper and lowercase letters, a number and at least 8 characters");
            }

            if (string.IsNullOrEmpty(item.ConfirmPassword))
            {
                validationResult.AddError(nameof(item.ConfirmPassword), "Please confirm password");
            }
            else if (!string.IsNullOrEmpty(item.Password) && !item.ConfirmPassword.Equals(item.Password))
            {
                validationResult.AddError(nameof(item.ConfirmPassword), "Sorry, your passwords don’t match");
            }

            if (!item.HasAcceptedTermsAndConditions)
            {
                validationResult.AddError(nameof(item.HasAcceptedTermsAndConditions), "Please accept the terms and conditions");
            }

            return validationResult;
        }

        private static bool CheckPasswordMatchesAtLeastOneUppercaseOneLowercaseOneNumberAndAtLeastEightCharacters(string password)
        {
            return !Regex.IsMatch(password, @"^(?=(.*[0-9].*))(?=(.*[a-z].*))(?=(.*[A-Z].*)).{8,}$");
        }
    }
}