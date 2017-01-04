using IdentityServer3.Core.Extensions;
using IdentityServer3.Core.Models;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace SFA.DAS.EmployerUsers.Web.Authentication
{
    public class OwinWrapper : IOwinWrapper
    {
        private readonly IOwinContext _owinContext;

        public OwinWrapper(IOwinContext owinContext)
        {
            _owinContext = owinContext;
        }

        public SignInMessage GetSignInMessage(string id)
        {
            return _owinContext.Environment.GetSignInMessage(id);
        }
        public void IssueLoginCookie(string id, string displayName)
        {
            var env = _owinContext.Environment;
            ClearSignInMessageCookie();
            env.IssueLoginCookie(new AuthenticatedLogin
            {
                Subject = id,
                Name = displayName
            });
        }

        public void ClearSignInMessageCookie()
        {
            foreach (var cookie in _owinContext.Request.Cookies)
            {
                if (cookie.Key.ToLower().StartsWith("signinmessage"))
                {
                    _owinContext.Response.Cookies.Delete(cookie.Key);
                }
            }
        }

        public void SetIdsContext(string returnUrl, string clientId)
        {
            var value = new IdsContext() {ReturnUrl = returnUrl, ClientId = clientId};
            var cookieOptions = new CookieOptions() {Secure = true};
            _owinContext.Response.Cookies.Append(IdsContext.CookieName, JsonConvert.SerializeObject(value), cookieOptions);;
        }

        public string GetIdsRedrect()
        {
            var cookie = IdsContext.ReadFrom(_owinContext.Request.Cookies[IdsContext.CookieName]);
            return cookie.ReturnUrl;
        }

        public void RemovePartialLoginCookie()
        {
            _owinContext.Environment.RemovePartialLoginCookie();
        }
    }

    public class IdsContext
    {
        public string ReturnUrl { get; set; }
        public string ClientId { get; set; }
        public static string CookieName => "IDS";

        public static IdsContext ReadFrom(string data)
        {
            return JsonConvert.DeserializeObject<IdsContext>(data);
        }
    }
}