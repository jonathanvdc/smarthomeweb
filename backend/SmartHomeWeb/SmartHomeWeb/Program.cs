﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SmartHomeWeb.Model;
using Newtonsoft.Json;

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
		const string DatabasePath = "../../../../database/smarthomeweb.db";
        private DataConnection Dc;

        public IndexModule()
        {
			Dc = DataConnection.CreateAsync(DatabasePath).Result;

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

			Get["/persons", true] = async (parameters, ct) =>
			{
				return "<html><body><table>" + string.Join("\n", (await Dc.GetPersonsAsync()).OrderBy(item => item.Id).Select(item => "<tr><td>" + item.Id + "</td><td>" + item.Data.Name + "</td></tr>")) + "</table></body></html>";
			};

			Post["/register_persons", true] = async (parameter, ct) =>
			{
				using (var textReader = new StreamReader(this.Request.Body))
				{
					string data = await textReader.ReadToEndAsync();
					var items = JsonConvert.DeserializeObject<List<PersonData>>(data);
					await Dc.InsertPersonsAsync(items);
					// return JsonConvert.SerializeObject(results.ToList());
					return "";
				}
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
