﻿using System;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.EmployerUsers.Domain;
using SFA.DAS.EmployerUsers.Domain.Data;

namespace SFA.DAS.EmployerUsers.Application.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IAsyncRequestHandler<GetUserByIdQuery, User>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<User> Handle(GetUserByIdQuery message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return _userRepository.GetById(message.UserId);
        }
    }
}
