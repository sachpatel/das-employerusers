﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Application.Queries.IsUserActive;
using SFA.DAS.EmployerUsers.Web.Authentication;
using SFA.DAS.EmployerUsers.Web.Orchestrators;

namespace SFA.DAS.EmployerUsers.Web.UnitTests.OrchestratorTests.AccountOrchestratorTests
{
    public class WhenRequestingConfirmAccount
    {
        private const string ActiveUserId = "ACTIVE_USER";
        private const string InactiveUserId = "INACTIVE_USER";

        private Mock<IMediator> _mediator;
        private Mock<IOwinWrapper> _owinWrapper;
        private AccountOrchestrator _orchestrator;

        [SetUp]
        public void Arrange()
        {
            _mediator = new Mock<IMediator>();
            _mediator.Setup(m => m.SendAsync(It.Is<IsUserActiveQuery>(q => q.UserId == InactiveUserId)))
                .Returns(Task.FromResult(false));
            _mediator.Setup(m => m.SendAsync(It.Is<IsUserActiveQuery>(q => q.UserId == ActiveUserId)))
                .Returns(Task.FromResult(true));

            _owinWrapper = new Mock<IOwinWrapper>();

            _orchestrator = new AccountOrchestrator(_mediator.Object, _owinWrapper.Object);
        }

        [Test]
        public async Task ThenItShouldReturnTrueIfUserIsNotActive()
        {
            // Act
            var actual = await _orchestrator.RequestConfirmAccount(InactiveUserId);

            // Assert
            Assert.IsTrue(actual);
        }

        [Test]
        public async Task ThenItShouldReturnFalseIfUserIsActive()
        {
            // Act
            var actual = await _orchestrator.RequestConfirmAccount(ActiveUserId);

            // Assert
            Assert.IsFalse(actual);
        }
    }
}
