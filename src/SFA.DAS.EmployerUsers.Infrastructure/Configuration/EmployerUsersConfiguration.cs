﻿namespace SFA.DAS.EmployerUsers.Infrastructure.Configuration
{
    public class EmployerUsersConfiguration
    {
        public virtual IdentityServerConfiguration IdentityServer { get; set; }
        public virtual AccountConfiguration Account { get; set; }
        public string Hashstring { get; set; }
    }
}
