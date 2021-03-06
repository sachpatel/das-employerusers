﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using Thinktecture.IdentityModel.Client;
using SFA.DAS.OidcMiddleware;

[assembly: OwinStartup(typeof(TestRP.Startup))]
namespace TestRP
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            JwtSecurityTokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies"
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "TempState",
                AuthenticationMode = AuthenticationMode.Passive
            });

            UseCodeOidcFlow(app);
        }

        private void UseImplicitOidcFlow(IAppBuilder app)
        {
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                Authority = "https://localhost:44334/identity/",
                ClientId = "testrp",
                Scope = "openid profile",
                ResponseType = "id_token token",
                RedirectUri = "http://localhost:17995/",
                SignInAsAuthenticationType = "Cookies",
                UseTokenLifetime = false,
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenValidated = async n =>
                    {
                        var nid = new ClaimsIdentity(
                            n.AuthenticationTicket.Identity.AuthenticationType,
                            DasClaimTypes.DisplayName,
                            "role");

                        // get userinfo data
                        var userInfoClient = new UserInfoClient(
                            new Uri(n.Options.Authority + "/connect/userinfo"),
                            n.ProtocolMessage.AccessToken);

                        var userInfo = await userInfoClient.GetAsync();
                        userInfo.Claims.ToList().ForEach(ui => nid.AddClaim(new Claim(ui.Item1, ui.Item2)));

                        // keep the id_token for logout
                        nid.AddClaim(new Claim("id_token", n.ProtocolMessage.IdToken));

                        // add access token for sample API
                        nid.AddClaim(new Claim("access_token", n.ProtocolMessage.AccessToken));

                        // keep track of access token expiration
                        nid.AddClaim(new Claim("expires_at",
                            DateTimeOffset.Now.AddSeconds(int.Parse(n.ProtocolMessage.ExpiresIn)).ToString()));

                        n.AuthenticationTicket = new AuthenticationTicket(
                            nid,
                            n.AuthenticationTicket.Properties);
                    },
                    RedirectToIdentityProvider = n =>
                    {
                        // Do not fiddle with api request please.
                        if (n.Request.Uri.AbsolutePath.ToLower().StartsWith("/api/"))
                        {
                            n.HandleResponse();
                            return Task.FromResult(0);
                        }

                        if (n.ProtocolMessage.RequestType == OpenIdConnectRequestType.LogoutRequest)
                        {
                            var idTokenHint = n.OwinContext.Authentication.User.FindFirst("id_token");

                            if (idTokenHint != null)
                            {
                                n.ProtocolMessage.IdTokenHint = idTokenHint.Value;
                            }
                        }

                        return Task.FromResult(0);
                    }
                }
            });
        }

        private void UseCodeOidcFlow(IAppBuilder app)
        {
            var baseUrl = "https://localhost:44334/identity";

            app.UseCodeFlowAuthentication(new OidcMiddlewareOptions
            {
                ClientId = "testrp",
                ClientSecret = "super-secret",
                Scopes = "openid profile",
                BaseUrl = baseUrl,
                TokenEndpoint = $"{baseUrl}/connect/token",
                UserInfoEndpoint = $"{baseUrl}/connect/userinfo",
                AuthorizeEndpoint = $"{baseUrl}/connect/authorize",
                TokenValidationMethod = TokenValidationMethod.SigningKey,
                TokenSigningCertificateLoader = () =>
                {
                    var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    try
                    {
                        var thumbprint = "6B7ACC520305BFDB4F7252DAEB2177CC091FAAE1";
                        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

                        if (certificates.Count < 1)
                        {
                            throw new Exception($"Could not find certificate with thumbprint {thumbprint} in LocalMachine store");
                        }

                        return certificates[0];
                    }
                    finally
                    {
                        store.Close();
                    }
                }
            });
        }
    }
}