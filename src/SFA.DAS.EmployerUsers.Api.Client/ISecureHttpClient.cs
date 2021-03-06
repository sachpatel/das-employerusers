﻿using System.Threading.Tasks;

namespace SFA.DAS.EmployerUsers.Api.Client
{
    public interface ISecureHttpClient
    {
        Task<string> GetAsync(string url);
    }
}
