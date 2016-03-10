using System;
using System.Collections.Generic;
using System.IO;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.TinyIoc;

namespace SmartHomeWeb
{
    class Program
    {
        private const string Domain = "http://localhost:8088";

        static void Main(string[] args)
        {
            var nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri(Domain), new Bootstrapper());
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
    public class RazorConfig : Nancy.ViewEngines.Razor.IRazorConfiguration
    {
        public IEnumerable<string> GetAssemblyNames()
        {
            yield return "libsmarthomeweb";
        }

        public IEnumerable<string> GetDefaultNamespaces() => null;

        public bool AutoIncludeModelNamespace => true;
    }
}
