using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeWeb
{
    class Program
    {
        const string Domain = "http://localhost:8088";

        static void Main(string[] args)
        {
            var nancyHost = new Nancy.Hosting.Self.NancyHost(new Uri(Domain));
            nancyHost.Start();
            Console.WriteLine("Running.");
            while (true)
            {
                IndexModule.PlatDuJour = Console.ReadLine();
            }
        }
    }

    public class IndexModule : Nancy.NancyModule
    {
        public static string PlatDuJour = "Yep. The server is running";

        public IndexModule()
        {
            Get["/"] = parameter => IndexPage;
            Get["/test/{x}"] =
                parameter => "<blink>" + parameter["x"] * 2.0;
        }

        public string IndexPage
        {
            get
            {
                return @"
                <html><body>
                <h1>" + PlatDuJour + @"</h1>
                </body></html>
                ";
            }
        }
    }


    // We may, at some point, need this:
    /*
    public class Bootstrapper : Nancy.DefaultNancyBootstrapper
    {
        protected virtual Nancy.Bootstrapper.NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                return Nancy.Bootstrapper.NancyInternalConfiguration.Default;
            }
        }
    }
    */
}
