using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SmartHomeWeb
{
    class Program
    {
        const string Domain = "http://localhost:8188";

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
            Get["/pages/{x}"] = parameter => Pages[parameter["x"]];
            Put["/pages/{x}", runAsync:true] = async (parameter, ct) =>
            {
                using (var textReader = new StreamReader(this.Request.Body))
                {
                    Pages[parameter["x"]] = await textReader.ReadToEndAsync();
                }
                return "";
            };
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

		private static Dictionary<string, string> Pages = new Dictionary<string, string>();
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
