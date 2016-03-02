using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
    class Program
    {
        private const string Domain = "http://localhost:8088";

        static void Main(string[] args)
        {
            Nancy.StaticConfiguration.DisableErrorTraces = false;
            var nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri(Domain), new Bootstrapper());
            nancyHost.Start();
            Console.WriteLine("Running from: " + Directory.GetCurrentDirectory());
            while (true)
            {
                SmartHomeWebModule.PlatDuJour = Console.ReadLine();
            }
        }
    }

    // We need a custom bootstrapper, because we want to modify Nancy's root path:
    public class Bootstrapper : Nancy.DefaultNancyBootstrapper
    {
        protected override Nancy.IRootPathProvider RootPathProvider
            => new CurrentDirectoryRootPathProvider();
    }

    // Namely, we want to use the working directory specified in Visual Studio. 
    public class CurrentDirectoryRootPathProvider : Nancy.IRootPathProvider
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
