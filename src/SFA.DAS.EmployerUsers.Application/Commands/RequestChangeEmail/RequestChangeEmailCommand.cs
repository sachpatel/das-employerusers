﻿using MediatR;

namespace SFA.DAS.EmployerUsers.Application.Commands.RequestChangeEmail
{
    public class RequestChangeEmailCommand : IAsyncRequest<RequestChangeEmailCommandResponse>
    {
        public string UserId { get; set; }
        public string NewEmailAddress { get; set; }
        public string ConfirmEmailAddress { get; set; }
        public string ReturnUrl { get; set; }
    }
}
