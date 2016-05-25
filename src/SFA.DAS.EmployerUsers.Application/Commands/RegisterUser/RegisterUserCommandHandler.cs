﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using SFA.DAS.EmployerUsers.Application.Services.Notification;
using SFA.DAS.EmployerUsers.Application.Services.Password;
using SFA.DAS.EmployerUsers.Domain;
using SFA.DAS.EmployerUsers.Domain.Data;

namespace SFA.DAS.EmployerUsers.Application.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : AsyncRequestHandler<RegisterUserCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICommunicationService _communicationService;
        private readonly IValidator<RegisterUserCommand> _registerUserCommandValidator;
        private readonly IPasswordService _passwordService;

        public RegisterUserCommandHandler(IValidator<RegisterUserCommand> registerUserCommandValidator, IPasswordService passwordService, IUserRepository userRepository, ICommunicationService communicationService)
        {
            _userRepository = userRepository;
            _communicationService = communicationService;
            _registerUserCommandValidator = registerUserCommandValidator;
            _passwordService = passwordService;
        }

        protected override async Task HandleCore(RegisterUserCommand message)
        {
            if (!_registerUserCommandValidator.Validate(message))
            {
                throw new InvalidRequestException(new[] { "NotValid" });
            }

            var securedPassword = await _passwordService.GenerateAsync(message.Password);

            var accessCode = "ABC123XYZ";
            var registerUser = new User
            {
                Id = message.Id,
                Email = message.Email,
                FirstName = message.FirstName,
                LastName = message.LastName,
                AccessCode = accessCode,
                Password = securedPassword.HashedPassword,
                Salt = securedPassword.Salt,
                PasswordProfileId = securedPassword.ProfileId
            };

            await _userRepository.Create(registerUser);
            
            await _communicationService.SendUserRegistrationMessage(registerUser);
            
        }
    }
    
}