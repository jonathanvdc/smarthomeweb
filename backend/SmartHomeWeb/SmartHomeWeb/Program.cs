using System;
using System.Collections.Generic;
using System.IO;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using Nancy.ViewEngines.Razor;

namespace SmartHomeWeb
{
    class Program
    {
        private const string Domain = "http://localhost:8088";

        static void Main(string[] args)
        {
			Console.WriteLine("Loaded resources:");
			foreach (var res in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames())
			{
				Console.WriteLine(res);
			}
            var nancyHost = new NancyHost(new Uri(Domain), new Bootstrapper());
            nancyHost.Start();
            Console.WriteLine("Running from: " + Directory.GetCurrentDirectory());
            while (true)
                Console.ReadLine();
        }
    }

    // We need a custom bootstrapper, because we want to modify Nancy's root path:
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            StaticConfiguration.DisableErrorTraces = false;
            StaticConfiguration.EnableRequestTracing = true;

            var formsAuthConfiguration = new FormsAuthenticationConfiguration
                {
                    RedirectUrl = "~/login",
                    UserMapper = container.Resolve<IUserMapper>()
                };
            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
        }

        protected override IRootPathProvider RootPathProvider
            => new CurrentDirectoryRootPathProvider();

        protected override DiagnosticsConfiguration DiagnosticsConfiguration =>
            new DiagnosticsConfiguration { Password = "admin" };
    }

    // Namely, we want to use the working directory specified in Visual Studio. 
    public class CurrentDirectoryRootPathProvider : IRootPathProvider
    {
        public string GetRootPath() => Directory.GetCurrentDirectory();
    }

    // We also want our views to know about the assembly containing our model.
    public class RazorConfig : IRazorConfiguration
    {
        public IEnumerable<string> GetAssemblyNames()
        {
            yield return "libsmarthomeweb";
        }

        public IEnumerable<string> GetDefaultNamespaces() => null;

        public bool AutoIncludeModelNamespace => true;
    }
}
