using System.Web.Mvc;
using System.Web.Routing;

namespace Tounaent_Fixtures
{
    public class RouteConfig
    {
        // Mirrors the two app.MapControllerRoute(...) calls in the original Program.cs exactly,
        // including the fact that the second ("home") route is effectively unreachable for "/"
        // since the first ("Default") route already matches it with its own defaults - that
        // was true in the ASP.NET Core version too, so behavior is preserved as-is rather than
        // "fixed" during the conversion.
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Auth", action = "Login", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Home",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
