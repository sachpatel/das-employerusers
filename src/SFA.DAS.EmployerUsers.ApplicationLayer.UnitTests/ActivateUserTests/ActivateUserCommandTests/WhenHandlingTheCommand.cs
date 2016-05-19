﻿using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.ApplicationLayer.Commands.ActivateUser;
using SFA.DAS.EmployerUsers.Domain.Data;

namespace SFA.DAS.EmployerUsers.ApplicationLayer.UnitTests.ActivateUserTests.ActivateUserCommandTests
{
    public class WhenHandlingTheCommand
    {
        private ActivateUserCommandHandler _activateUserCommand;
        private Mock<IValidator<ActivateUserCommand>> _activateUserCommandValidator;
        private Mock<IUserRepository> _userRepository;

        [SetUp]
        public void Arrange()
        {
            _activateUserCommandValidator = new Mock<IValidator<ActivateUserCommand>>();
            _userRepository = new Mock<IUserRepository>();
            _userRepository.Setup(x => x.GetById(It.IsAny<string>())).ReturnsAsync(new Domain.User());
            _activateUserCommand = new ActivateUserCommandHandler(_activateUserCommandValidator.Object, _userRepository.Object);
        }

        [Test]
        public async Task ThenActivateUserCommandValidatorIsCalled()
        {
            //Arrange
            _activateUserCommandValidator.Setup(x => x.Validate(It.IsAny<ActivateUserCommand>())).Returns(true);

            //Act
            await _activateUserCommand.Handle(new ActivateUserCommand());

            //Assert
            _activateUserCommandValidator.Verify(x=>x.Validate(It.IsAny<ActivateUserCommand>()),Times.Once);
        }

        [Test]
        public async Task ThenTheUserRepositoryIsCalledIfTheCommandIsValid()
        {
            //Arrange
            _activateUserCommandValidator.Setup(x => x.Validate(It.IsAny<ActivateUserCommand>())).Returns(true);

            //Act
            await _activateUserCommand.Handle(new ActivateUserCommand());

            //Assert
            _userRepository.Verify(x=>x.Update(It.IsAny<Domain.User>()),Times.Once);

        }


        [Test]
        public void ThenTheUserRepositoryIsNotCalledIfTheCommandIsInValid()
        {
            //Arrange
            _activateUserCommandValidator.Setup(x => x.Validate(It.IsAny<ActivateUserCommand>())).Returns(false);

            //Act
            Assert.ThrowsAsync<InvalidRequestException>(async () => await _activateUserCommand.Handle(new ActivateUserCommand()));
            
            //Assert
            _userRepository.Verify(x => x.Update(It.IsAny<Domain.User>()), Times.Never);
        }


        [Test]
        public void ThenAInvalidDataExceptionIsThrownIfTheCommandIsInValid()
        {
            //Arrange
            _activateUserCommandValidator.Setup(x => x.Validate(It.IsAny<ActivateUserCommand>())).Returns(false);

            //Act
            Assert.ThrowsAsync<InvalidRequestException>(async () => await _activateUserCommand.Handle(new ActivateUserCommand()));
            
            //Assert
            _userRepository.Verify(x => x.Update(It.IsAny<Domain.User>()), Times.Never);
        }


        [Test]
        public async Task ThenTheUserIsRetrievedFromTheUserRepositoryIfTheCommandIsValid()
        {
            //Arrange
            var userId = Guid.NewGuid().ToString();
            _activateUserCommandValidator.Setup(x => x.Validate(It.IsAny<ActivateUserCommand>())).Returns(true);

            //Act
            await _activateUserCommand.Handle(new ActivateUserCommand {UserId = userId });

            //Assert
            _userRepository.Verify(x => x.GetById(userId), Times.Once);
        }

        [Test]
        public async Task ThenThenUserIsUpdatedWithTheCorrectDetails()
        {
            //Arrange
            var userId = Guid.NewGuid().ToString();
            var accessCode = "123ADF&^%";
            var user = new Domain.User
            {
                Email = "test@test.com",
                LastName = "Tester",
                FirstName = "Test",
                Password = "SomePassword",
                IsActive = false,
                Id = userId,
                AccessCode = accessCode
            };
            var activateUserCommand = new ActivateUserCommand
            {
                UserId = userId,
                AccessCode = accessCode
            };
            _userRepository.Setup(x => x.GetById(userId)).ReturnsAsync(user);
            _activateUserCommandValidator.Setup(x => x.Validate(It.IsAny<ActivateUserCommand>())).Returns(true);

            //Act
            await _activateUserCommand.Handle(activateUserCommand);

            //Assert
            _userRepository.Verify(x => x.Update(It.Is<Domain.User>(p=>p.IsActive && p.Id == userId)), Times.Once);
        }
    }
}
