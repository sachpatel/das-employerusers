﻿using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Application.Queries.GetUserById;
using SFA.DAS.EmployerUsers.Domain;
using SFA.DAS.EmployerUsers.Domain.Data;

namespace SFA.DAS.EmployerUsers.Application.UnitTests.QueriesTests.GetUserByIdTests.GetUserByIdQueryHandlerTests
{
    public class WhenHandlingRequest
    {
        private User _user;
        private Mock<IUserRepository> _userRepository;
        private GetUserByIdQueryHandler _handler;
        private GetUserByIdQuery _query;

        [SetUp]
        public void Arrange()
        {
            _user = new User
            {
                Id = "xyz",
                IsActive = true
            };
            _userRepository = new Mock<IUserRepository>();
            _userRepository.Setup(r => r.GetById("xyz")).Returns(Task.FromResult(_user));

            _handler = new GetUserByIdQueryHandler(_userRepository.Object);

            _query = new GetUserByIdQuery { UserId = "xyz" };
        }

        [Test]
        public async Task ThenItShouldReturnTheUserIfFound()
        {
            // Act
            var actual = await _handler.Handle(_query);

            // Assert
            Assert.AreSame(_user, actual);
        }

        [Test]
        public async Task ThenItShouldReturnNullIfUserNotFound()
        {
            // Arrange
            _query.UserId = "zyx";

            // Act
            var actual = await _handler.Handle(_query);

            // Assert
            Assert.IsNull(actual);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfQueryIsNull()
        {
            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () => await _handler.Handle(null));
            Assert.AreEqual("message", ex.ParamName);
        }

    }
}
