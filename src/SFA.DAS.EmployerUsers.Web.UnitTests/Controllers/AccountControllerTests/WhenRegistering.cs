﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerUsers.Web.Controllers;
using SFA.DAS.EmployerUsers.Web.Models;
using SFA.DAS.EmployerUsers.Web.Models.SFA.DAS.EAS.Web.Models;
using SFA.DAS.EmployerUsers.Web.Orchestrators;

namespace SFA.DAS.EmployerUsers.Web.UnitTests.Controllers.AccountControllerTests
{
    public class WhenRegistering : ControllerTestBase
    {
        private const string ClientId = "MyClient";
        private const string ReturnUrl = "https://localhost/identity/connect/authorize?p1=somestuff";

        private AccountController _accountController;
        private Mock<AccountOrchestrator> _accountOrchestator;
        private Mock<UrlHelper> _urlHelper;
        
        [SetUp]
        public override void Arrange()
        {
            base.Arrange();
            

            _accountOrchestator = new Mock<AccountOrchestrator>();
            _accountOrchestator.Setup(o => o.StartRegistration(ClientId, ReturnUrl, true))
                .ReturnsAsync(new RegisterViewModel());

            _urlHelper = new Mock<UrlHelper>();
            _urlHelper.Setup(h => h.Action("Index", "Home", null, "https"))
                .Returns("https://localhost/");

            _accountController = new AccountController(_accountOrchestator.Object, null, null, _logger.Object);
            _accountController.ControllerContext = _controllerContext.Object;
            _accountController.Url = _urlHelper.Object;
        }
        
        [Test]
        public async Task ThenTheAccountOrchestratorRegisterIsCalled()
        {
            //Arrange
            _accountOrchestator.Setup(x => x.Register(It.IsAny<RegisterViewModel>(), It.IsAny<string>()))
                .ReturnsAsync(new OrchestratorResponse<RegisterViewModel> {Data = new RegisterViewModel(), FlashMessage = new FlashMessageViewModel()});

            //Act
            await _accountController.Register(new RegisterViewModel(), ReturnUrl);

            //Assert
            _accountOrchestator.Verify(x => x.Register(It.IsAny<RegisterViewModel>(), ReturnUrl));
        }

        [Test]
        public async Task ThenTheConfirmViewIsReturnedWhenTheOrchestratorReturnsTrue()
        {
            //Arrange
            AddUserToContext();
            _accountOrchestator.Setup(x => x.Register(It.IsAny<RegisterViewModel>(), It.IsAny<string>()))
                .ReturnsAsync(new OrchestratorResponse<RegisterViewModel> { Data = new RegisterViewModel() });

            //Act
            var actual = await _accountController.Register(new RegisterViewModel(), ReturnUrl);

            //Assert
            Assert.IsNotNull(actual);
            var redirectToRouteResult = actual as RedirectToRouteResult;
            Assert.IsNotNull(redirectToRouteResult);
            Assert.AreEqual("Confirm", redirectToRouteResult.RouteValues["Action"].ToString());

        }

        [Test]
        public async Task ThenTheRegisterViewIsReturnedWhenTheOrchestratorReturnsFalse()
        {
            //Arrange
            _accountOrchestator.Setup(x => x.Register(It.IsAny<RegisterViewModel>(), It.IsAny<string>()))
                .ReturnsAsync(new OrchestratorResponse<RegisterViewModel> {Data=new RegisterViewModel(), FlashMessage = new FlashMessageViewModel {ErrorMessages = new Dictionary<string, string> { { "Error", "Error" } } }});

            //Act
            var actual = await _accountController.Register(new RegisterViewModel(), ReturnUrl);

            //Assert
            Assert.IsNotNull(actual);
            var actualViewResult = actual as ViewResult;
            Assert.IsNotNull(actualViewResult);
            Assert.AreEqual("Register", actualViewResult.ViewName);
            Assert.IsAssignableFrom<OrchestratorResponse<RegisterViewModel>>(actualViewResult.Model);
            var actualModel = actualViewResult.Model as OrchestratorResponse<RegisterViewModel>;
            Assert.IsNotNull(actualModel);
        }
        
        [Test]
        public async Task ThenIAmRedirectedToTheConfirmCodeIfITryToReSubmitMyRegistrationWhenLoggedIn()
        {
            //Arrange
            AddUserToContext("123456");
            _accountController.ControllerContext = _controllerContext.Object;
            _accountOrchestator.Setup(x => x.Register(It.IsAny<RegisterViewModel>(), It.IsAny<string>()))
                .ReturnsAsync(new OrchestratorResponse<RegisterViewModel> { Data = new RegisterViewModel() });

            //Act
            var actual = await _accountController.Register(new RegisterViewModel(), ReturnUrl);

            //Assert
            _accountOrchestator.Verify(x => x.Register(It.IsAny<RegisterViewModel>(), It.IsAny<string>()), Times.Never);
            Assert.IsNotNull(actual);
            var redirectToRouteResult = actual as RedirectToRouteResult;
            Assert.IsNotNull(redirectToRouteResult);
            Assert.AreEqual("Confirm", redirectToRouteResult.RouteValues["Action"].ToString());

        }

        [Test]
        public async Task ThenTheErrorMessageIrlIsSubstituedIfTheUserAlreadyExists()
        {
            //Arrange
            var errors = new Dictionary<string, string> {{"Email","<a href='__loginurl__'>Login to your account</a>"}};
            _accountOrchestator.Setup(x => x.Register(It.IsAny<RegisterViewModel>(), It.IsAny<string>()))
                .ReturnsAsync(new OrchestratorResponse<RegisterViewModel> { Data = new RegisterViewModel(),FlashMessage = new FlashMessageViewModel {ErrorMessages = errors} });

            //Act
            var actual = await _accountController.Register(new RegisterViewModel(), ReturnUrl);

            //Assert
            Assert.IsNotNull(actual);
            var actualViewResult = actual as ViewResult;
            Assert.IsNotNull(actualViewResult);
            var actualModel = actualViewResult.Model as OrchestratorResponse<RegisterViewModel>;
            Assert.IsNotNull(actualModel);
            Assert.AreEqual("<a href=''>Login to your account</a>", actualModel.FlashMessage.ErrorMessages["Email"]);
        }
    }
}
