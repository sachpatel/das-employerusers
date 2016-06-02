﻿using System.Threading.Tasks;
using MediatR;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Application;
using SFA.DAS.EmployerUsers.Application.Commands.ResendActivationCode;
using SFA.DAS.EmployerUsers.Web.Authentication;
using SFA.DAS.EmployerUsers.Web.Models;
using SFA.DAS.EmployerUsers.Web.Orchestrators;

namespace SFA.DAS.EmployerUsers.Web.UnitTests.OrchestratorTests.AccountOrchestratorTests
{
    [TestFixture]
    public class WhenResendingActivationCode
    {
        private Mock<IMediator> _mediator;
        private Mock<IOwinWrapper> _owinWrapper;
        private AccountOrchestrator _accountOrchestrator;

        [SetUp]
        public void Setup()
        {
            _mediator = new Mock<IMediator>();
            _owinWrapper = new Mock<IOwinWrapper>();

            _accountOrchestrator = new AccountOrchestrator(_mediator.Object, _owinWrapper.Object);
        }

        [Test]
        public async Task ThenTrueIfMediatorCallSuceeds()
        {
            var actual = await _accountOrchestrator.ResendActivationCode(new ResendActivationCodeViewModel());

            Assert.True(actual);
        }

        [Test]
        public async Task ThenFalseIfMediatorThrowsException()
        {
            _mediator.Setup(x => x.SendAsync(It.IsAny<ResendActivationCodeCommand>())).ThrowsAsync(new InvalidRequestException(new[] { "Something" }));

            var actual = await _accountOrchestrator.ResendActivationCode(new ResendActivationCodeViewModel());

            Assert.False(actual);
        }
    }
}