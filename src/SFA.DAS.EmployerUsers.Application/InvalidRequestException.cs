﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SFA.DAS.EmployerUsers.Application
{
    public class InvalidRequestException : Exception
    {
        public  Dictionary<string,string> ErrorMessages { get; private set; }

        public InvalidRequestException(Dictionary<string,string> errorMessages)
            : base(BuildErrorMessage(errorMessages))
        {
            this.ErrorMessages = errorMessages;
        }

        private static string BuildErrorMessage(Dictionary<string, string> errorMessages)
        {
            return "Request is invalid:\n"
                   + errorMessages.Select(kvp => $"{kvp.Key}: {kvp.Value}").Aggregate((x, y) => $"{x}\n{y}");
        }
    }
}
