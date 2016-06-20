﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Application.Commands.PasswordReset;
using SFA.DAS.EmployerUsers.Application.Validation;
using SFA.DAS.EmployerUsers.Domain;
using SFA.DAS.EmployerUsers.Domain.Data;

namespace SFA.DAS.EmployerUsers.Application.UnitTests.CommandsTests.PasswordResetTests.PasswordResetCommandTests
{
    public class WhenHandlingTheCommand
    {
        private Mock<IUserRepository> _userRepository;
        private Mock<IValidator<PasswordResetCommand>> _validator;
        private PasswordResetCommandHandler _passwordResetCommandHandler;
        private const string ActualEmailAddress = "someuser@local";
        public string PasswordResetCode = "123456ABC";

        [SetUp]
        public void Arrange()
        {
            _userRepository = new Mock<IUserRepository>();
            _userRepository.Setup(x => x.GetByEmailAddress(It.IsAny<string>())).ReturnsAsync(null);
            _userRepository.Setup(x => x.GetByEmailAddress(ActualEmailAddress)).ReturnsAsync(new User { Email = ActualEmailAddress, PasswordResetCode = PasswordResetCode });
            
            _validator = new Mock<IValidator<PasswordResetCommand>>();
            _validator.Setup(x => x.Validate(It.IsAny<PasswordResetCommand>())).Returns(new ValidationResult { ValidationDictionary = new Dictionary<string, string>()});

            _passwordResetCommandHandler = new PasswordResetCommandHandler(_userRepository.Object, _validator.Object);
        }

        [Test]
        public async Task ThenTheUserIsReturnedFromTheRespository()
        {
            //Act
            await _passwordResetCommandHandler.Handle(new PasswordResetCommand {Email = ActualEmailAddress});

            //Assert
            _userRepository.Verify(x=>x.GetByEmailAddress(ActualEmailAddress));

        }

        [Test]
        public async Task ThenTheValidatorIsCalled()
        {
            //Act
            await _passwordResetCommandHandler.Handle(new PasswordResetCommand { Email = ActualEmailAddress });

            //Assert
            _validator.Verify(x=>x.Validate(It.IsAny<PasswordResetCommand>()),Times.Once);
        }

        [Test]
        public async Task ThenTheMessageIsPopulatedwithUserAndValidated()
        {
            //Act
            await _passwordResetCommandHandler.Handle(new PasswordResetCommand { Email = ActualEmailAddress });

            //Assert
            _validator.Verify(x => x.Validate(It.Is<PasswordResetCommand>(c=>c.User!=null)), Times.Once);
        }

        [Test]
        public async Task ThenTheUserIsUpdatedIfTheValidatorIsValid()
        {
            //Act
            await _passwordResetCommandHandler.Handle(new PasswordResetCommand { Email = ActualEmailAddress,Password = "somePassword", ConfirmPassword = "someConfirmPassword"});

            //Assert
            _userRepository.Verify(x => x.Update(It.Is<User>(c=>c.Email == ActualEmailAddress && c.Password == "somePassword" && c.PasswordResetCode=="" && c.PasswordResetCodeExpiry == null)), Times.Once);
        }

        [Test]
        public void ThenAInvliadRequestExceptionIsThrownIfTheMessageIsNotValid()
        {
            //Arrange
            _validator.Setup(x => x.Validate(It.IsAny<PasswordResetCommand>())).Returns(new ValidationResult {ValidationDictionary = new Dictionary<string, string> {{"", ""}}});

            //Act
            Assert.ThrowsAsync<InvalidRequestException>(async () => await _passwordResetCommandHandler.Handle(new PasswordResetCommand()));

            //Assert
            _userRepository.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public void ThenTheUserIsNotUpdatedIfTheValidatorIsInValid()
        {
            //Arrange
            _validator.Setup(x => x.Validate(It.IsAny<PasswordResetCommand>())).Returns(new ValidationResult { ValidationDictionary = new Dictionary<string, string> { { "", "" } } });

            //Act
            Assert.ThrowsAsync<InvalidRequestException>(async () => await _passwordResetCommandHandler.Handle(new PasswordResetCommand { Email = "someotheremail@local" }));

            //Assert
            _userRepository.Verify(x => x.Update(It.IsAny<User>()),Times.Never);
        }
    }
}