﻿namespace SFA.DAS.EmployerUsers.Infrastructure.Configuration
{
    public class EmployerUsersConfiguration
    {
        public IdentityServerConfiguration IdentityServer { get; set; }
    }

    public class IdentityServerConfiguration
    {
        public string CertificateStore { get; set; }
        public string CertificateThumbprint { get; set; }
    }
}