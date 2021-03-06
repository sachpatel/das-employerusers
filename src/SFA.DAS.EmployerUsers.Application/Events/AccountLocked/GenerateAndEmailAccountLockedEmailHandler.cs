﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Azure;
using NLog;
using SFA.DAS.CodeGenerator;
using SFA.DAS.Configuration;
using SFA.DAS.EmployerUsers.Application.Services.Notification;
using SFA.DAS.EmployerUsers.Domain.Auditing;
using SFA.DAS.EmployerUsers.Domain.Auditing.Registration;
using SFA.DAS.EmployerUsers.Domain.Data;
using SFA.DAS.EmployerUsers.Infrastructure.Configuration;

namespace SFA.DAS.EmployerUsers.Application.Events.AccountLocked
{
    public class GenerateAndEmailAccountLockedEmailHandler : IAsyncNotificationHandler<AccountLockedEvent>
    {
        private readonly ILogger _logger;

        private readonly IConfigurationService _configurationService;
        private readonly IUserRepository _userRepository;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ICommunicationService _communicationService;
        private readonly IAuditService _auditService;

        public GenerateAndEmailAccountLockedEmailHandler(IConfigurationService configurationService, IUserRepository userRepository, ICodeGenerator codeGenerator, ICommunicationService communicationService, IAuditService auditService, ILogger logger)
        {
            _configurationService = configurationService;
            _userRepository = userRepository;
            _codeGenerator = codeGenerator;
            _communicationService = communicationService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(AccountLockedEvent notification)
        {
            _logger.Debug($"Handling AccountLockedEvent for user '{notification.User?.Email}' (id: {notification.User?.Id})");

            var user = !string.IsNullOrEmpty(notification.User.Id)
                            ? await _userRepository.GetById(notification.User.Id)
                            : await _userRepository.GetByEmailAddress(notification.User.Email);

            if (user == null)
            {
                return;
            }

            var sendNotification = false;

            var unlockCode = user.SecurityCodes?.OrderByDescending(sc => sc.ExpiryTime)
                                                .FirstOrDefault(sc => sc.CodeType == Domain.SecurityCodeType.UnlockCode);

            if (unlockCode == null || unlockCode.ExpiryTime < DateTime.UtcNow
                && CloudConfigurationManager.GetSetting("UseStaticCodeGenerator").Equals("false", StringComparison.CurrentCultureIgnoreCase))
            {
                unlockCode = new Domain.SecurityCode
                {
                    Code = await GenerateCode(),
                    CodeType = Domain.SecurityCodeType.UnlockCode,
                    ExpiryTime = DateTime.UtcNow.AddDays(1),
                    ReturnUrl = notification.ReturnUrl
                };
                user.AddSecurityCode(unlockCode);
                await _userRepository.Update(user);

                _logger.Debug($"Generated new unlock code of '{unlockCode.Code}' for user '{user.Id}'");
                sendNotification = true;
            }

            if (notification.ResendUnlockCode || sendNotification)
            {
                await _communicationService.SendAccountLockedMessage(user, Guid.NewGuid().ToString());

                await _auditService.WriteAudit(new SendUnlockCodeAuditMessage(user, unlockCode));
            }
        }

        private async Task<string> GenerateCode()
        {
            var codeLength = (await GetConfig())?.UnlockCodeLength ?? 6;
            return _codeGenerator.GenerateAlphaNumeric(codeLength);
        }
        private async Task<AccountConfiguration> GetConfig()
        {
            return (await _configurationService.GetAsync<EmployerUsersConfiguration>())?.Account;
        }
    }
}
