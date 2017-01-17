﻿using System;
using Moq;
using NLog;
using NUnit.Framework;
using SFA.DAS.Configuration;
using SFA.DAS.EmployerUsers.Infrastructure.Configuration;

namespace SFA.DAS.EmployerUsers.Infrastructure.UnitTests.ConfigurationTests.EmployerUsersClientComponentConfigurationFactoryTests
{
    public class WhenGettingConfiguration
    {
        private Mock<IConfigurationService> _configurationService;
        private EmployerUsersClientComponentConfigurationFactory _factory;
        private Mock<ILogger> _logger;

        [SetUp]
        public void Arrange()
        {
            _configurationService = new Mock<IConfigurationService>();
            _configurationService.Setup(s => s.Get<EmployerUsersConfiguration>())
                .Returns(
                    new EmployerUsersConfiguration
                    {
                        IdentityServer = new IdentityServerConfiguration
                        {
                            ApplicationBaseUrl = "http://unittests.local/"
                        }
                    });

            _logger = new Mock<ILogger>();

            _factory = new EmployerUsersClientComponentConfigurationFactory(_configurationService.Object,_logger.Object);
        }

        [Test]
        public void ThenItShouldReturnAccountActivationUrlPointingToConfirm()
        {
            // Act
            var actual = _factory.Get();

            // Assert
            Assert.AreEqual("http://unittests.local/account/confirm/", actual.AccountActivationUrl);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfConfigurationServiceFails()
        {
            // Arrange
            _configurationService.Setup(s => s.Get<EmployerUsersConfiguration>()).Throws<Exception>();

            // Act + Assert
            Assert.Throws<Exception>(() => _factory.Get());

        }
    }
}