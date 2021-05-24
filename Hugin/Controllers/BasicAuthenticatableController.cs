using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hugin.Models;
using Hugin.Data;
using Hugin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hugin.Controllers
{

    public abstract class BasicAuthenticatableController : ControllerBase
    {
        protected UserHandleService UserHandler { get; set; }
        public BasicAuthenticatableController(UserHandleService userHandler)
        {
            UserHandler = userHandler;
        }

        protected IActionResult BasicAuthFiltered(Func<User, bool> auth, Func<User, IActionResult> callback)
        {
            var appUrl = Environment.GetEnvironmentVariable("APP_URL") ?? "";
            var realm = appUrl;
            string authHeader = HttpContext.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic ", StringComparison.CurrentCulture))
            {
                var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                var account = decodedUsernamePassword.Split(':', 2)[0];
                var password = decodedUsernamePassword.Split(':', 2)[1];
                var (result, user) = authenticate(account, password);
                if (result && auth(user))
                {
                    return callback(user);
                }
            }
            HttpContext.Response.Headers["WWW-Authenticate"] = "Basic";
            if (!string.IsNullOrWhiteSpace(realm))
            {
                HttpContext.Response.Headers["WWW-Authenticate"] += $" realm=\"{realm}\"";
            }
            return new UnauthorizedResult();
        }
        protected IActionResult BasicAuthFiltered(Func<User, bool> auth, Func<User, Task<IActionResult>> callback)
        {
            return BasicAuthFiltered(auth, user => callback(user).Result);
        }
        protected virtual (bool, User) authenticate(string account, string password)
        {
            var user = UserHandler.Set.Where(x => x.Account == account).Include(x => x.LectureUserRelationships).ThenInclude(x => x.Lecture).AsNoTracking().FirstOrDefault();
            if (user == null) return (false, null);
            return (UserHandler.Authenticate(user, password), user);
        }
    }
}
