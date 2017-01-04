﻿using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SFA.DAS.EmployerUsers.Web.Authentication;
using IdentityServer3.Core;
using IdentityServer3.Core.Extensions;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using SFA.DAS.Configuration;
using SFA.DAS.EmployerUsers.Infrastructure.Configuration;
using SFA.DAS.EmployerUsers.Web.Models;
using SFA.DAS.EmployerUsers.Web.Models.SFA.DAS.EAS.Web.Models;
using SFA.DAS.EmployerUsers.Web.Orchestrators;
using SFA.DAS.EmployerUsers.WebClientComponents;

namespace SFA.DAS.EmployerUsers.Web.Controllers
{
    public class AccountController : ControllerBase
    {
        private readonly AccountOrchestrator _accountOrchestrator;
        private readonly IOwinWrapper _owinWrapper;
        private readonly IConfigurationService _configurationService;

        public AccountController(AccountOrchestrator accountOrchestrator, IOwinWrapper owinWrapper, IConfigurationService configurationService)
        {
            _accountOrchestrator = accountOrchestrator;
            _owinWrapper = owinWrapper;
            _configurationService = configurationService;
        }



        [HttpGet]
        [Route("identity/employer/login")]
        public ActionResult Login(string id, string clientId)
        {
            
            var signinMessage = _owinWrapper.GetSignInMessage(id);
            _owinWrapper.SetIdsContext(signinMessage.ReturnUrl, clientId);

            var model = new OrchestratorResponse<LoginViewModel>
            {
                Data = new LoginViewModel
                {
                    ReturnUrl = signinMessage.ReturnUrl,
                    ClientId = clientId
                }
            };
            if (TempData["AccountUnlocked"] != null)
            {
                model.FlashMessage = new FlashMessageViewModel()
                {

                    Severity = FlashMessageSeverityLevel.Success,
                    Message = "Account Unlocked",
                    SubMessage =
                        "Your account has been unlocked, if you can't remember your password use the Forgotten Password link below"
                };
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("identity/employer/login")]
        public async Task<ActionResult> Login(string id, LoginViewModel model)
        {
            model.OriginatingAddress = Request.UserHostAddress;
            var result = await _accountOrchestrator.Login(model);
            var response = new OrchestratorResponse<LoginViewModel>();

            if (result.Data.Success)
            {
                if (result.Data.RequiresActivation)
                {
                    return RedirectToAction("Confirm");
                }

                var signinMessage = _owinWrapper.GetSignInMessage(id);
                return Redirect(signinMessage.ReturnUrl);
            }

            if (result.Data.AccountIsLocked)
            {
                
                return RedirectToAction("Unlock");
            }

            if (result.Status != HttpStatusCode.OK)
            {
                response.Data = new LoginViewModel
                {
                    ReturnUrl = model.ReturnUrl
                };
                response.FlashMessage = result.FlashMessage;
                response.Status = result.Status;
                response.Data.ErrorDictionary = result.FlashMessage.ErrorMessages;
                response.Status = HttpStatusCode.OK;
                

                return View(response);
            }

            return View(response);
        }



        [Route("account/logout")]
        public ActionResult Logout()
        {
            Request.GetOwinContext().Authentication.SignOut();
            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        [Route("account/register")]
        [OutputCache(Duration = 0)]
        [AttemptAuthorise]
        public async Task<ActionResult> Register(string clientId, string returnUrl)
        {
            var loginReturnUrl = Url.Action("Index", "Home", null, Request.Url.Scheme)
                                 + "identity/connect/authorize";
            var isLocalReturnUrl = returnUrl.ToLower().StartsWith(loginReturnUrl.ToLower());
            var model = await _accountOrchestrator.StartRegistration(clientId, returnUrl, isLocalReturnUrl);
            if (!model.Valid)
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            var id = GetLoggedInUserId();

            if (!string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Confirm");
            }

            _owinWrapper.ClearSignInMessageCookie();

            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("identity/employer/register")]
        [OutputCache(Duration = 0)]
        public async Task<ActionResult> Register(RegisterViewModel model, string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest);
            }
            var id = GetLoggedInUserId();

            if (!string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Confirm");
            }

            var registerResult = await _accountOrchestrator.Register(model, returnUrl);

            if (registerResult.Valid)
            {
                return RedirectToAction("Confirm");
            }

            model.ConfirmPassword = string.Empty;
            model.Password = string.Empty;
            model.ReturnUrl = returnUrl;

            return View("Register", model);
        }


        [HttpGet]
        [Authorize]
        [Route("account/confirm")]
        public async Task<ActionResult> Confirm()
        {
            var userId = GetLoggedInUserId();
            var confirmationRequired = await _accountOrchestrator.RequestConfirmAccount(userId);
            if (!confirmationRequired)
            {

                return RedirectToAction("Index", "Home");
            }
            return View("Confirm", new OrchestratorResponse<ActivateUserViewModel>() { Data = new ActivateUserViewModel { Valid = true }});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("account/confirm")]
        public async Task<ActionResult> Confirm(ActivateUserViewModel activateUserViewModel, string command)
        {
            var id = GetLoggedInUserId();

            if (command.Equals("activate"))
            {
                activateUserViewModel =
                    await
                        _accountOrchestrator.ActivateUser(new ActivateUserViewModel
                        {
                            AccessCode = activateUserViewModel.AccessCode,
                            UserId = id
                        });

                if (activateUserViewModel.Valid)
                {
                    return Redirect(activateUserViewModel.ReturnUrl);
                }

                return View("Confirm", new OrchestratorResponse<ActivateUserViewModel>() { Data = new ActivateUserViewModel { Valid = false }});
            }
            else
            {
                var result = await _accountOrchestrator.ResendActivationCode(new ResendActivationCodeViewModel { UserId = id });

                var flashMessage = new FlashMessageViewModel()
                {
                    Severity = FlashMessageSeverityLevel.Success,
                    Headline = "We've sent you an email",
                    SubMessage = $"To confirm your identity, we've sent a code to {GetLoggedInUserEmail()}"
                };
                return View("Confirm", new OrchestratorResponse<ActivateUserViewModel>() {FlashMessage = flashMessage, Data = new ActivateUserViewModel { Valid = result }});
            }
        }



        [HttpGet]
        [AttemptAuthorise]
        [Route("account/unlock")]
        public ActionResult Unlock()
        {
            var email = GetLoggedInUserEmail();
            var model = new UnlockUserViewModel { Email = email };
            return View("Unlock", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/unlock")]
        public async Task<ActionResult> Unlock(UnlockUserViewModel unlockUserViewModel, string command)
        {

            if (command.ToLower() == "resend")
            {
                var result = await _accountOrchestrator.ResendUnlockCode(unlockUserViewModel);

                return View("Unlock", result);
            }
            else
            {
                var result = await _accountOrchestrator.UnlockUser(unlockUserViewModel);

                if (result.Valid)
                {
                    if (!string.IsNullOrEmpty(result.ReturnUrl))
                    {
                        TempData["AccountUnlocked"] = true;
                        return new RedirectResult(result.ReturnUrl);
                    }
                    return await RedirectToEmployerPortal();
                }
                unlockUserViewModel.UnlockCode = string.Empty;
                return View("Unlock", unlockUserViewModel);
            }
        }



        [HttpGet]
        [Route("account/forgottencredentials")]
        public async Task<ActionResult> ForgottenCredentials(string clientId)
        {
            var model = await _accountOrchestrator.StartForgottenPassword(clientId);

            if (!model.Valid)
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            return View("ForgottenCredentials", model);
        }

        [Route("account/resetflow")]
        public ActionResult ResetFlow()
        {
            var returnUrl = _owinWrapper.GetIdsRedrect();
            
            return Redirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/forgottencredentials")]
        public async Task<ActionResult> ForgottenCredentials(RequestPasswordResetViewModel requestPasswordResetViewModel, string clientId)
        {
         
            requestPasswordResetViewModel.ClientId = clientId;
            requestPasswordResetViewModel = await _accountOrchestrator.RequestPasswordResetCode(requestPasswordResetViewModel);

            if (string.IsNullOrEmpty(requestPasswordResetViewModel.Email) || !requestPasswordResetViewModel.Valid)
            {
                return View("ForgottenCredentials", requestPasswordResetViewModel);
            }


            return View("ResetPassword", new PasswordResetViewModel { Email = requestPasswordResetViewModel.Email });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("identity/employer/resetpassword")]
        public async Task<ActionResult> ResetPassword(PasswordResetViewModel model)
        {
            model = await _accountOrchestrator.ResetPassword(model);

            if (model.Valid)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl))
                {
                    var returnUrl =_owinWrapper.GetIdsRedrect();
                    return new RedirectResult(returnUrl);
                }
                return await RedirectToEmployerPortal();
            }

            return View("ResetPassword", model);
        }



        [HttpGet]
        [Authorize]
        [Route("account/changeemail")]
        public async Task<ActionResult> ChangeEmail(string clientId, string returnUrl)
        {
            var model = await _accountOrchestrator.StartRequestChangeEmail(clientId, returnUrl);
            //if (!model.Data.Valid)
            //{
            //    return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest);
            //}
            return View(model);
        }

        public async Task<ActionResult> ResendActivation()
        {
            var newEmailAddress = TempData["EmailChangeNewEmail"] as string;
            var clientId = TempData["EmailChangeClientId"] as string;
            var returnUrl = TempData["EmailChangeReturnUrl"] as string;
   

           await ChangeEmail(new ChangeEmailViewModel() {ConfirmEmailAddress = newEmailAddress, NewEmailAddress = newEmailAddress},clientId, returnUrl);

            TempData["EmailChangeRequested"] = true;
            TempData["EmailChangeNewEmail"] = newEmailAddress;
  
            TempData["EmailChangeReturnUrl"] = returnUrl;
            TempData["EmailChangeClientId"] = clientId;

            return RedirectToAction("ConfirmChangeEmail");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("account/changeemail")]
        public async Task<ActionResult> ChangeEmail(ChangeEmailViewModel model, string clientId, string returnUrl)
        {
            model.UserId = GetLoggedInUserId();
            model.ClientId = clientId;
            model.ReturnUrl = returnUrl;

            var response = await _accountOrchestrator.RequestChangeEmail(model);

            if (response.Status == HttpStatusCode.BadRequest)
            {
                return View("ChangeEmail", response);
            }
            TempData["EmailChangeRequested"] = true;
            TempData["EmailChangeNewEmail"] = model.NewEmailAddress;
          
            TempData["EmailChangeReturnUrl"] = returnUrl;
            TempData["EmailChangeClientId"] = clientId;

            return RedirectToAction("ConfirmChangeEmail");
        }

        [HttpGet]
        [Authorize]
        [Route("account/confirmchangeemail")]
        public ActionResult ConfirmChangeEmail()
        {
            var email = TempData["EmailChangeNewEmail"];

            var model = new OrchestratorResponse<ConfirmChangeEmailViewModel>
            {
                Data = new ConfirmChangeEmailViewModel(),
                FlashMessage = new FlashMessageViewModel()
                {
                    Severity = FlashMessageSeverityLevel.Success,
                    Headline = "Check your email",
                    SubMessage = email != null ? $"We've sent a security code to {TempData["EmailChangeNewEmail"]}" : "We've sent you a security code"
                }
            };

            model.Data.UserId = GetLoggedInUserId();


            TempData["EmailChangeRequested"] = true;
            TempData["EmailChangeNewEmail"] = TempData["EmailChangeNewEmail"] as string;
        
            TempData["EmailChangeReturnUrl"] = TempData["EmailChangeReturnUrl"];
            TempData["EmailChangeClientId"] = TempData["EmailChangeClientId"];


            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("account/confirmchangeemail")]
        public async Task<ActionResult> ConfirmChangeEmail(ConfirmChangeEmailViewModel model)
        {
            model.UserId = GetLoggedInUserId();

            model = await _accountOrchestrator.ConfirmChangeEmail(model);
            if (model.Valid)
            {
                return Redirect(model.ReturnUrl);
            }

            model.SecurityCode = string.Empty;
            model.Password = string.Empty;
            return View(new OrchestratorResponse<ConfirmChangeEmailViewModel>() { Data = model });
        }


        [HttpGet]
        [Authorize]
        [Route("account/changepassword")]
        public async Task<ActionResult> ChangePassword(string clientId, string returnUrl)
        {

            var model = await _accountOrchestrator.StartChangePassword(clientId, returnUrl);
            //if (!model.Valid)
            //{
            //    return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest);
            //}
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("account/changepassword")]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model, string clientId, string returnUrl)
        {
            model.UserId = GetLoggedInUserId();

            model = await _accountOrchestrator.ChangePassword(model);
            if (model.Valid)
            {
                return Redirect(returnUrl);
            }

            model.CurrentPassword = string.Empty;
            model.NewPassword = string.Empty;
            model.ConfirmPassword = string.Empty;
            return View(new OrchestratorResponse<ChangePasswordViewModel>() {Data = model});
        }




        private string GetLoggedInUserId()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var idClaim = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == DasClaimTypes.Id);
            if (idClaim == null)
            {
                idClaim = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == Constants.ClaimTypes.Subject);
            }
            var id = idClaim?.Value;
            return id;
        }

        private string GetLoggedInUserEmail()
        {
            var claimsIdentity = User?.Identity as ClaimsIdentity;
            var idClaim = claimsIdentity?.Claims?.FirstOrDefault(c => c.Type == DasClaimTypes.Email);

            var id = idClaim?.Value;
            return id;
        }


        private async Task<ActionResult> RedirectToEmployerPortal()
        {
            var configuration = await _configurationService.GetAsync<EmployerUsersConfiguration>();
            return Redirect(configuration.IdentityServer.EmployerPortalUrl);
        }

 
    }
}