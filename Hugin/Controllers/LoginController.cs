using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Hugin.Data;
using Hugin.Services;
using Hugin.ViewModels;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ALMS.App.Controllers
{

    [AllowAnonymous]
    public class LoginController : Controller
    {
        UserHandleService UserHandler;
        public LoginController(UserHandleService userHandler)
        {
            UserHandler = userHandler;
        }

        [HttpGet("logout", Name = "Logout")]
        public async Task<ActionResult> Logout()
        {
            var pathBase = HttpContext.Request.PathBase.HasValue ? HttpContext.Request.PathBase.Value : "";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect($"{pathBase}/Login");
        }

        [HttpGet("login", Name = "Login")]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }


        [HttpPost("login")]
        public async Task<ActionResult> Login([FromQuery]string ReturnUrl, LoginViewModel m)
        {
            var pathBase = HttpContext.Request.PathBase.HasValue ? HttpContext.Request.PathBase.Value : "";
            if (!ModelState.IsValid)
            {
                return View(m);
            }

            User user = UserHandler.Set.Where(x => x.Account == m.Account).FirstOrDefault();
            if (user == null)
            {
                ViewData["ErrorMessage"] = "The user is not exist.";
                return View(m);
            }
            var isSuccess = UserHandler.Authenticate(user, m.Password);
            if (!isSuccess)
            {
                ViewData["ErrorMessage"] = "Authentication failed.";
                return View(m);
            }
            if(user.IsLdapUser && !user.IsLdapInitialized)
            {
                user.IsLdapInitialized = true;
                UserHandler.Update(user);
            }

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.Account));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            if (user.IsTeacher)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Teacher"));
            }
            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
              CookieAuthenticationDefaults.AuthenticationScheme,
              new ClaimsPrincipal(claimsIdentity),
              new AuthenticationProperties
              {
                  IsPersistent = m.RememberMe
              });

            return LocalRedirect(ReturnUrl ?? $"{pathBase}/");
        }
    }
}
