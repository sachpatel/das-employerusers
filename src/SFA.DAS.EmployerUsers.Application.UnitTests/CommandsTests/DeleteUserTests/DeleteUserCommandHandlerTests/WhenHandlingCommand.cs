﻿using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Application.Commands.DeleteUser;
using SFA.DAS.EmployerUsers.Domain;
using SFA.DAS.EmployerUsers.Domain.Auditing;
using SFA.DAS.EmployerUsers.Domain.Auditing.Delete;
using SFA.DAS.EmployerUsers.Domain.Data;

namespace SFA.DAS.EmployerUsers.Application.UnitTests.CommandsTests.DeleteUserTests.DeleteUserCommandHandlerTests
{
    public class WhenHandlingCommand
    {
        private const string UserId = "USER1";
        private Mock<IUserRepository> _userRepository;
        private DeleteUserCommandHandler _handler;
        private DeleteUserCommand _command;
        private Mock<IAuditService> _auditService;

        [SetUp]
        public void Arrange()
        {
            _userRepository = new Mock<IUserRepository>();

            _auditService = new Mock<IAuditService>();

            _handler = new DeleteUserCommandHandler(_userRepository.Object, _auditService.Object);

            _command = new DeleteUserCommand
            {
                User = new User
                {
                    Id = UserId
                }
            };
        }

        [Test]
        public async Task ThenItShouldDeleteUserFromRepo()
        {
            // Act
            await _handler.Handle(_command);

            // Assert
            _userRepository.Verify(r => r.Delete(It.Is<User>(u => u.Id == UserId)), Times.Once);
        }

        [Test]
        public async Task ThenItShouldAuditDeletion()
        {
            // Act
            await _handler.Handle(_command);

            // Assert
            _auditService.Verify(s => s.WriteAudit(It.IsAny<DeleteUserAuditMessage>()), Times.Once);
        }
    }
}
