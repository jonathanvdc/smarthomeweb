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
        private const string Domain = "http://localhost:8088";
        
        static void Main(string[] args)
        {
			Nancy.StaticConfiguration.DisableErrorTraces = false;
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
        private DataConnection Dc;

        public IndexModule()
        {
            Dc = new DataConnection();

            Get["/"] = parameter => IndexPage;
            Get["/test/{x}"] = parameter => "<blink>" + parameter["x"] * 2.0;
            Get["/pages/{x}"] = parameter => Pages[parameter["x"]];
            Put["/pages/{x}", runAsync:true] = async (parameter, ct) =>
            {
                using (var textReader = new StreamReader(this.Request.Body))
                {
                    Pages[parameter["x"]] = await textReader.ReadToEndAsync();
                }
                return "<h1>u did it";
            };

            Get["/testview", true] = async (parameters, ct) =>
            {
                var persons = await Dc.GetPersonsAsync();
                return View["../../../../../frontend/views/testy.sshtml", persons];
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
