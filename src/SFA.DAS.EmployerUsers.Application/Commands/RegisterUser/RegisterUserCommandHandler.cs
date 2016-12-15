﻿using System;
using System.Threading.Tasks;
using MediatR;
using NLog;
using SFA.DAS.CodeGenerator;
using SFA.DAS.EmployerUsers.Application.Services.Notification;
using SFA.DAS.EmployerUsers.Application.Services.Password;
using SFA.DAS.EmployerUsers.Application.Validation;
using SFA.DAS.EmployerUsers.Domain;
using SFA.DAS.EmployerUsers.Domain.Data;
using System.Collections.Generic;
using System.Linq;

namespace SFA.DAS.EmployerUsers.Application.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : AsyncRequestHandler<RegisterUserCommand>
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly IUserRepository _userRepository;
        private readonly ICommunicationService _communicationService;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IValidator<RegisterUserCommand> _registerUserCommandValidator;
        private readonly IPasswordService _passwordService;

        public RegisterUserCommandHandler(IValidator<RegisterUserCommand> registerUserCommandValidator,
            IPasswordService passwordService,
            IUserRepository userRepository,
            ICommunicationService communicationService,
            ICodeGenerator codeGenerator)
        {
            _userRepository = userRepository;
            _communicationService = communicationService;
            _codeGenerator = codeGenerator;
            _registerUserCommandValidator = registerUserCommandValidator;
            _passwordService = passwordService;
        }

        protected override async Task HandleCore(RegisterUserCommand message)
        {
            Logger.Debug($"Received RegisterUserCommand for user '{message.Email}'");

            var validationResult = await _registerUserCommandValidator.ValidateAsync(message);

            if (!validationResult.IsValid())
            {
                throw new InvalidRequestException(validationResult.ValidationDictionary);
            }

            var existingUser = await _userRepository.GetByEmailAddress(message.Email);

            if (existingUser != null && existingUser.IsActive)
            {
                throw new InvalidRequestException(new Dictionary<string, string>
                {
                    {
                        nameof(message.Email),
                        "Your email address has already been activated. Please try signing in again. If you've forgotten your password you can reset it."
                    }
                });
            }

            var securedPassword = await _passwordService.GenerateAsync(message.Password);

            if (existingUser == null)
            {
                var registerUser = Create(message, securedPassword);

                await _userRepository.Create(registerUser);
                await SendUserRegistrationMessage(registerUser);
            }
            else
            {
                Update(existingUser, message, securedPassword);

                await _userRepository.Update(existingUser);
                await SendUserRegistrationMessage(existingUser);
            }
        }

        private async Task SendUserRegistrationMessage(User user)
        {
            await _communicationService.SendUserRegistrationMessage(user, Guid.NewGuid().ToString());
        }

        private void Update(User user, RegisterUserCommand message, SecuredPassword securedPassword)
        {
            user.FirstName = message.FirstName;
            user.LastName = message.LastName;
            user.Password = securedPassword.HashedPassword;
            user.Salt = securedPassword.Salt;
            user.PasswordProfileId = securedPassword.ProfileId;
            user.PasswordHistory = (user.PasswordHistory ?? new HistoricalPassword[0]).Concat(new[]
             {
                new HistoricalPassword
                {
                    Password = user.Password,
                    Salt = user.Salt,
                    PasswordProfileId = user.PasswordProfileId,
                    DateSet = DateTime.Now
                }
            }).ToArray();
        }

        private User Create(RegisterUserCommand message, SecuredPassword securedPassword)
        {
            var user = new User
            {
                Id = message.Id,
                Email = message.Email,
                SecurityCodes = new[]
                {
                    new SecurityCode
                    {
                        Code = _codeGenerator.GenerateAlphaNumeric(),
                        CodeType = SecurityCodeType.AccessCode,
                        ExpiryTime =   DateTime.Today.AddDays(8).AddSeconds(-1),
                        ReturnUrl = message.ReturnUrl
                    }
                }
            };

            Update(user, message, securedPassword);

            return user;
        }
    }
}