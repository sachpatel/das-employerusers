﻿using System.Threading.Tasks;
using MediatR;
using SFA.DAS.EmployerUsers.Domain.Data;

namespace SFA.DAS.EmployerUsers.Application.Commands.ActivateUser
{
    public class ActivateUserCommandHandler : AsyncRequestHandler<ActivateUserCommand>
    {
        private readonly IValidator<ActivateUserCommand> _activateUserCommandValidator;
        private readonly IUserRepository _userRepository;


        public ActivateUserCommandHandler(IValidator<ActivateUserCommand> activateUserCommandValidator, IUserRepository userRepository)
        {
            _activateUserCommandValidator = activateUserCommandValidator;
            _userRepository = userRepository;
        }

        protected override async Task HandleCore(ActivateUserCommand message)
        {
            var user = await _userRepository.GetById(message.UserId);
            message.User = user;
            var result = _activateUserCommandValidator.Validate(message);

            if (!result)
            {
                throw new InvalidRequestException(new[] { "NotValid" });
            }

            user.IsActive = true;

            await _userRepository.Update(user); 
        }
    }
}