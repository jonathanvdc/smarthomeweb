﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.ViewEngines.Razor;
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
            Console.WriteLine("Running from:");
            Console.WriteLine(Directory.GetCurrentDirectory());
            while (true)
            {
                SmartHomeWebModule.PlatDuJour = Console.ReadLine();
            }
        }
    }

    public class Bootstrapper : Nancy.DefaultNancyBootstrapper
    {
        protected override IRootPathProvider RootPathProvider => new CurrentDirectoryRootPathProvider();
    }

    public class CurrentDirectoryRootPathProvider : Nancy.IRootPathProvider
    {
        public string GetRootPath() => Directory.GetCurrentDirectory();
    }

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
