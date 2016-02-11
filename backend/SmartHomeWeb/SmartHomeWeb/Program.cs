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
            }
        }
    }

    public class IndexModule : Nancy.NancyModule
    {
        public IndexModule()
        {
            Get["/"] = parameter => IndexPage;
            Get["/test/{x}"] =
                parameter => "<blink>" + parameter["x"] * 2;
        }

        const string IndexPage = @"
            <html><body>
            <h1>Yep. The server is running</h1>
            </body></html>
            ";
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
