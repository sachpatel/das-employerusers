﻿using System;
using System.Threading.Tasks;
using MediatR;
using NLog;
using SFA.DAS.EmployerUsers.Application;
using SFA.DAS.EmployerUsers.Application.Commands.ActivateUser;
using SFA.DAS.EmployerUsers.Application.Commands.AuthenticateUser;
using SFA.DAS.EmployerUsers.Application.Commands.ChangeEmail;
using SFA.DAS.EmployerUsers.Application.Commands.ChangePassword;
using SFA.DAS.EmployerUsers.Application.Commands.PasswordReset;
using SFA.DAS.EmployerUsers.Application.Commands.RegisterUser;
using SFA.DAS.EmployerUsers.Application.Commands.RequestChangeEmail;
using SFA.DAS.EmployerUsers.Application.Commands.RequestPasswordResetCode;
using SFA.DAS.EmployerUsers.Application.Commands.ResendActivationCode;
using SFA.DAS.EmployerUsers.Application.Commands.ResendUnlockCode;
using SFA.DAS.EmployerUsers.Application.Commands.UnlockUser;
using SFA.DAS.EmployerUsers.Application.Queries.GetUserByEmailAddress;
using SFA.DAS.EmployerUsers.Application.Queries.GetUserById;
using SFA.DAS.EmployerUsers.Application.Queries.IsUserActive;
using SFA.DAS.EmployerUsers.Web.Authentication;
using SFA.DAS.EmployerUsers.Web.Models;

namespace SFA.DAS.EmployerUsers.Web.Orchestrators
{
    public class AccountOrchestrator
    {
        

        private readonly IMediator _mediator;
        private readonly IOwinWrapper _owinWrapper;
        private readonly ILogger _logger;


        //Needed for testing
        protected AccountOrchestrator()
        {

        }

        public AccountOrchestrator(IMediator mediator, IOwinWrapper owinWrapper, ILogger logger)
        {
            _mediator = mediator;
            _owinWrapper = owinWrapper;
            _logger = logger;
        }

        public virtual async Task<LoginResultModel> Login(LoginViewModel loginViewModel)
        {
            try
            {
                var user = await _mediator.SendAsync(new AuthenticateUserCommand
                {
                    EmailAddress = loginViewModel.EmailAddress,
                    Password = loginViewModel.Password
                });
                if (user == null)
                {
                    _logger.Warn($"Failed login attempt for email address '{loginViewModel.EmailAddress}' originating from {loginViewModel.OriginatingAddress}");
                    return new LoginResultModel { Success = false };
                }

                LoginUser(user.Id, user.FirstName, user.LastName);

                return new LoginResultModel { Success = true, RequiresActivation = !user.IsActive };
            }
            catch (AccountLockedException ex)
            {
                _logger.Info(ex.Message);
                return new LoginResultModel { AccountIsLocked = true };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return new LoginResultModel { Success = false };
            }
        }

        public virtual async Task<RegisterViewModel> Register(RegisterViewModel registerUserViewModel, string returnUrl)
        {
            try
            {
                await _mediator.SendAsync(new RegisterUserCommand
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = registerUserViewModel.FirstName,
                    LastName = registerUserViewModel.LastName,
                    Email = registerUserViewModel.Email,
                    Password = registerUserViewModel.Password,
                    ConfirmPassword = registerUserViewModel.ConfirmPassword,
                    HasAcceptedTermsAndConditions = registerUserViewModel.HasAcceptedTermsAndConditions,
                    ReturnUrl = returnUrl
                });

                var user = await _mediator.SendAsync(new GetUserByEmailAddressQuery
                {
                    EmailAddress = registerUserViewModel.Email
                });

                LoginUser(user.Id, user.FirstName, user.LastName);
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info(ex, ex.Message);
                registerUserViewModel.ErrorDictionary = ex.ErrorMessages;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                registerUserViewModel.ErrorDictionary = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"", "Unexpected error occured"}
                };
            }

            return registerUserViewModel;
        }

        public virtual async Task<ActivateUserViewModel> ActivateUser(ActivateUserViewModel model)
        {
            try
            {
                var result = await _mediator.SendAsync(new ActivateUserCommand
                {
                    AccessCode = model.AccessCode,
                    UserId = model.UserId
                });

                model.Valid = true;
                model.ReturnUrl = result.ReturnUrl;
                return model;
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info(ex, ex.Message);

                model.Valid = false;
                return model;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);

                model.Valid = false;
                return model;
            }
        }

        public virtual async Task<bool> ResendActivationCode(ResendActivationCodeViewModel resendActivationCodeViewModel)
        {
            try
            {
                await _mediator.SendAsync(new ResendActivationCodeCommand
                {
                    UserId = resendActivationCodeViewModel.UserId
                });

                return true;
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info(ex, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return false;
            }
        }

        public virtual async Task<bool> RequestConfirmAccount(string userId)
        {
            var isUserActive = await _mediator.SendAsync(new IsUserActiveQuery { UserId = userId });
            return !isUserActive;
        }

        public virtual async Task<UnlockUserViewModel> UnlockUser(UnlockUserViewModel unlockUserViewModel)
        {
            try
            {
                await _mediator.SendAsync(new UnlockUserCommand
                {
                    Email = unlockUserViewModel.Email,
                    UnlockCode = unlockUserViewModel.UnlockCode
                });

                await _mediator.SendAsync(new ActivateUserCommand
                {
                    Email = unlockUserViewModel.Email
                });

                return unlockUserViewModel;
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info(ex, ex.Message);

                if (ex.ErrorMessages.ContainsKey(nameof(unlockUserViewModel.UnlockCodeExpired)))
                {
                    unlockUserViewModel.UnlockCodeExpired = true;
                }
                unlockUserViewModel.ErrorDictionary = ex.ErrorMessages;
                return unlockUserViewModel;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                unlockUserViewModel.ErrorDictionary = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"", "Unexpected error occured"}
                };
                return unlockUserViewModel;
            }
        }

        public virtual async Task<UnlockUserViewModel> ResendUnlockCode(UnlockUserViewModel model)
        {

            try
            {
                await _mediator.SendAsync(new ResendUnlockCodeCommand
                {
                    Email = model.Email
                });

                model.UnlockCodeSent = true;

                return model;
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info(ex, ex.Message);
                model.ErrorDictionary = ex.ErrorMessages;
                return model;
            }

        }

        public virtual async Task<RequestPasswordResetViewModel> RequestPasswordResetCode(RequestPasswordResetViewModel model)
        {
            try
            {
                await _mediator.SendAsync(new RequestPasswordResetCodeCommand
                {
                    Email = model.Email
                });

                model.ResetCodeSent = true;

                return model;
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info(ex, ex.Message);
                model.ErrorDictionary = ex.ErrorMessages;
                return model;
            }
        }

        public virtual async Task<PasswordResetViewModel> ResetPassword(PasswordResetViewModel model)
        {
            try
            {
                await _mediator.SendAsync(new PasswordResetCommand
                {
                    Email = model.Email,
                    PasswordResetCode = model.PasswordResetCode,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword
                });

                var user = await _mediator.SendAsync(new GetUserByEmailAddressQuery
                {
                    EmailAddress = model.Email
                });

                LoginUser(user.Id, user.FirstName, user.LastName);

                return model;
            }
            catch (InvalidRequestException ex)
            {
                _logger.Info(ex, ex.Message);
                model.ErrorDictionary = ex.ErrorMessages;
                model.Password = string.Empty;
                model.ConfirmPassword = string.Empty;
                return model;
            }
        }

        public virtual async Task<ChangeEmailViewModel> RequestChangeEmail(ChangeEmailViewModel model)
        {
            try
            {
                await _mediator.SendAsync(new RequestChangeEmailCommand
                {
                    UserId = model.UserId,
                    NewEmailAddress = model.NewEmailAddress,
                    ConfirmEmailAddress = model.ConfirmEmailAddress,
                    ReturnUrl = model.ReturnUrl
                });
            }
            catch (InvalidRequestException ex)
            {
                model.ErrorDictionary = ex.ErrorMessages;
            }
            catch (Exception ex)
            {
                model.ErrorDictionary.Add("", ex.Message);
            }
            return model;
        }

        public virtual async Task<ConfirmChangeEmailViewModel> ConfirmChangeEmail(ConfirmChangeEmailViewModel model)
        {
            try
            {
                var user = await _mediator.SendAsync(new GetUserByIdQuery
                {
                    UserId = model.UserId
                });

                var changeEmailResult = await _mediator.SendAsync(new ChangeEmailCommand
                {
                    User = user,
                    SecurityCode = model.SecurityCode,
                    Password = model.Password
                });
                model.ReturnUrl = changeEmailResult.ReturnUrl;
            }
            catch (InvalidRequestException ex)
            {
                model.ErrorDictionary = ex.ErrorMessages;
            }
            catch (Exception ex)
            {
                model.ErrorDictionary.Add("", ex.Message);
            }
            return model;
        }

        public virtual async Task<ChangePasswordViewModel> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                var user = await _mediator.SendAsync(new GetUserByIdQuery
                {
                    UserId = model.UserId
                });

                await _mediator.SendAsync(new ChangePasswordCommand
                {
                    User = user,
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword
                });
            }
            catch (InvalidRequestException ex)
            {
                model.ErrorDictionary = ex.ErrorMessages;
            }
            catch (Exception ex)
            {
                model.ErrorDictionary.Add("", ex.Message);
            }

            return model;
        }



        private void LoginUser(string id, string firstName, string lastName)
        {
            _owinWrapper.IssueLoginCookie(id, $"{firstName} {lastName}");

            _owinWrapper.RemovePartialLoginCookie();
        }

    }
}