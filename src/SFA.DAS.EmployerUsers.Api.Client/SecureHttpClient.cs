﻿using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace SFA.DAS.EmployerUsers.Api.Client
{
    internal class SecureHttpClient : ISecureHttpClient
    {
        private readonly IEmployerUsersApiConfiguration _configuration;

        internal SecureHttpClient(IEmployerUsersApiConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<AuthenticationResult> GetAuthenticationResult(string clientId, string appKey, string resourceId, string tenant)
        {
            var authority = $"https://login.microsoftonline.com/{tenant}";
            var clientCredential = new ClientCredential(clientId, appKey);
            var context = new AuthenticationContext(authority, true);
            var result = await context.AcquireTokenAsync(resourceId, clientCredential);
            return result;
        }

        public virtual async Task<string> GetAsync(string url)
        {
            var authenticationResult = await GetAuthenticationResult(_configuration.ClientId, _configuration.ClientSecret, _configuration.IdentifierUri, _configuration.Tenant);

            using (var client = new HttpClient())
            using (var store = new ClientCertificateStore(new X509Store(StoreLocation.LocalMachine)))
            using (var handler = new WebRequestHandler())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);

                var certificate = store.FindCertificateByThumbprint(_configuration.ClientCertificateThumbprint);
                if (certificate != null)
                {
                    handler.ClientCertificates.Add(certificate);
                }

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}