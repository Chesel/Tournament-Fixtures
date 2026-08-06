using System;
using System.IO;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using Tounaent_Fixtures.Models;

namespace Tounaent_Fixtures
{
    // Replaces Program.cs. ASP.NET Core's minimal hosting model (WebApplication.CreateBuilder)
    // has no equivalent on .NET Framework, so startup goes back to the classic
    // Global.asax Application_Start pattern.
    public class MvcApplication : System.Web.HttpApplication
    {
        // Exposed so controllers/helpers that used to take IConfiguration by constructor
        // injection still can, via the Autofac container registration below.
        public static AppConfig Configuration { get; private set; } = null!;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Equivalent of builder.Configuration in Program.cs
            Configuration = new AppConfig(AppDomain.CurrentDomain.BaseDirectory);

            // Equivalent of builder.Services.AddDbContext<ApplicationDbContext>(...) +
            // AddControllersWithViews() DI registration. MVC5 has no built-in container,
            // so Autofac takes over as the DependencyResolver.
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            builder.RegisterInstance(Configuration).As<AppConfig>().SingleInstance();
            builder.RegisterType<ApplicationDbContext>().InstancePerRequest();

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

            // Note: builder.Services.AddDataProtection() from Program.cs was dropped here -
            // UrlEncryptionHelper uses plain System.Security.Cryptography.Aes directly and
            // never actually consumed IDataProtectionProvider, so nothing depended on it.
        }
    }
}
