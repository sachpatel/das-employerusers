﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Infrastructure.Configuration;
using SFA.DAS.EmployerUsers.Web.Authentication;
using SFA.DAS.EmployerUsers.Web.Controllers;
using SFA.DAS.EmployerUsers.Web.Models;
using SFA.DAS.EmployerUsers.Web.Orchestrators;

namespace SFA.DAS.EmployerUsers.Web.UnitTests.Controllers.AccountControllerTests
{
    public class WhenUnlockingMyAccount : ControllerTestBase
    {
        private AccountController _accountController;
        private Mock<AccountOrchestrator> _accountOrchestrator;
        private Mock<IOwinWrapper> _owinWrapper;
        private const string LoggedInEmail = "local@test.com";
        private const string EmployerPortalUrl = "employerportal";

        [SetUp]
        public override void Arrange()
        {
            base.Arrange();

            
            AddUserToContext("USER_ID", LoggedInEmail);

            _accountOrchestrator = new Mock<AccountOrchestrator>();
            _owinWrapper = new Mock<IOwinWrapper>();

            var identityServerConfiguration = new IdentityServerConfiguration {EmployerPortalUrl = EmployerPortalUrl};
            _accountController = new AccountController(_accountOrchestrator.Object,_owinWrapper.Object, identityServerConfiguration, _logger.Object);
            _accountOrchestrator.Setup(x => x.UnlockUser(It.IsAny<UnlockUserViewModel>())).ReturnsAsync(new OrchestratorResponse<UnlockUserViewModel>() { Data = new UnlockUserViewModel {ErrorDictionary = new Dictionary<string, string>() }});
            _accountController.ControllerContext = _controllerContext.Object;
        }

        [Test]
        public void ThenTheUnlockViewIsReturned()
        {
            //Act
            var actual = _accountController.Unlock();

            //Assert
            Assert.IsNotNull(actual);
            var viewResult = actual as ViewResult;
            Assert.IsNotNull(viewResult);
            Assert.AreEqual("Unlock",viewResult.ViewName);
            Assert.IsAssignableFrom<OrchestratorResponse<UnlockUserViewModel>>(viewResult.Model);
        }

        [Test]
        public void ThenMyEmailAddressIsPopulatedInTheModelIfImLoggedIn()
        {

            //Act
            var actual = _accountController.Unlock();

            //Assert
            Assert.IsNotNull(actual);
            var viewResult = actual as ViewResult;
            Assert.IsNotNull(viewResult);
            var actualModel = viewResult.Model as OrchestratorResponse<UnlockUserViewModel>; 
            Assert.IsNotNull(actualModel);
            Assert.AreEqual(LoggedInEmail, actualModel.Data.Email);
        }

        [Test]
        public void ThenMyEmailIsNotPopulatedIfIAmNotLoggedIn()
        {
            //Arrange
            _accountController.ControllerContext = null;

            //Act
            var actual = _accountController.Unlock();

            //Assert
            Assert.IsNotNull(actual);
            var viewResult = actual as ViewResult;
            Assert.IsNotNull(viewResult);
            var actualModel = viewResult.Model as OrchestratorResponse<UnlockUserViewModel>;
            Assert.IsNotNull(actualModel);
            Assert.IsNull(actualModel.Data.Email);
        }

        [Test]
        public async Task ThenWhenPostedTheOrchestratorIsCalled()
        {
            //Arrange
            var unlockCode = "123RET678";
            var unlockUserViewModel = new UnlockUserViewModel {Email = LoggedInEmail, UnlockCode = unlockCode};

            //Act
            await _accountController.Unlock(unlockUserViewModel);

            //Assert
            _accountOrchestrator.Verify(x=>x.UnlockUser(It.Is<UnlockUserViewModel>(c=>c.Email == LoggedInEmail && c.UnlockCode == unlockCode)));
        }

        [Test]
        public async Task ThenTheUserIsRedirectedToTheEmployerPortalIfTheOrchestratorIsSuccessful()
        {
            //Arrange
            var unlockCode = "123RET678";
            var unlockUserViewModel = new UnlockUserViewModel { Email = LoggedInEmail, UnlockCode = unlockCode };

            //Act
            var actual = await _accountController.Unlock(unlockUserViewModel);

            //Assert
            Assert.IsNotNull(actual);
            var viewResult = actual as RedirectResult;
            Assert.IsNotNull(viewResult);
            Assert.AreEqual(EmployerPortalUrl, viewResult.Url);
        }

        [Test]
        public async Task ThenTheUserIsReturnedToTheUnlockViewIfTheOrchestratorIsNotSuccessful()
        {
            //Arrange
            _accountOrchestrator.Setup(x => x.UnlockUser(It.IsAny<UnlockUserViewModel>())).ReturnsAsync(new OrchestratorResponse<UnlockUserViewModel> { Data = new UnlockUserViewModel { ErrorDictionary = new Dictionary<string, string> { {"",""} } }});
            var unlockCode = "123RET678";
            var unlockUserViewModel = new UnlockUserViewModel { Email = LoggedInEmail, UnlockCode = unlockCode };

            //Act
            var actual = await _accountController.Unlock(unlockUserViewModel);

            //Assert
            Assert.IsNotNull(actual);
            var viewResult = actual as ViewResult;
            Assert.IsNotNull(viewResult);
            Assert.AreEqual("Unlock", viewResult.ViewName);
        }

        [Test]
        public async Task ThenTheResendUnlockCodeMethodIsCalledWhenTheCommandIsSetToResend()
        {
            //Arrange
            var unlockUserViewModel = new UnlockUserViewModel();
            _accountOrchestrator.Setup(x => x.ResendUnlockCode(It.IsAny<UnlockUserViewModel>())).ReturnsAsync(new OrchestratorResponse<UnlockUserViewModel> { Data = new UnlockUserViewModel { UnlockCodeSent = true}});

            //Act
            var actual = await _accountController.RequestUnlockCode(unlockUserViewModel);

            //Assert
            _accountOrchestrator.Verify(x=>x.ResendUnlockCode(unlockUserViewModel),Times.Once);
            Assert.IsNotNull(actual);
            var viewResult = actual as ViewResult;
            Assert.IsNotNull(viewResult);
            var model = viewResult.Model as OrchestratorResponse<UnlockUserViewModel>;
            Assert.IsNotNull(model);
            Assert.IsTrue(model.Data.UnlockCodeSent);
        }
        
    }
}
