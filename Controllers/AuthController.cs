using System.Web.Mvc;
using System.Web.Security;
using Tounaent_Fixtures.Models;

namespace Tounaent_Fixtures.Controllers
{
    public class AuthController : Controller
    {
        // IActionResult (ASP.NET Core) -> ActionResult (MVC5).
        [HttpGet]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // Microsoft.AspNetCore.Authentication.Cookies (CookieAuthenticationDefaults, ClaimsIdentity,
        // HttpContext.SignInAsync) has no equivalent on .NET Framework MVC5. The direct replacement
        // is FormsAuthentication, which is what System.Web has always used for this - it writes the
        // same kind of encrypted auth cookie and is synchronous, so the method no longer needs
        // to be async.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Username == "admin" && model.Password == "password")
                {
                    // NOTE: this hardcoded admin/password check was already in the original code.
                    // It's flagged here because it's a real login gate for a live app - worth
                    // replacing with a real credential check (e.g. against a Users table) rather
                    // than carrying it forward as-is.
                    FormsAuthentication.SetAuthCookie(model.Username, createPersistentCookie: false);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Invalid username or password");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Auth");
        }
    }
}
