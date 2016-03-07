using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SmartHomeWeb.Model;
using Nancy.Security;

namespace SmartHomeWeb.Modules
{
    public class SmartHomeWebModule : Nancy.NancyModule
    {
        public static string PlatDuJour = "Yep. The server is running";

        public SmartHomeWebModule()
        {
            Get["/"] = parameter => IndexPage;
            Get["/test/{x}"] = parameter => "<blink>" + parameter["x"] * 2.0;
            Get["/pages/{x}"] = parameter => Pages[parameter["x"]];

            
            Put["/pages/{x}", runAsync:true] = async (parameter, ct) =>
            {
                using (var textReader = new StreamReader(Request.Body))
                {
                    Pages[parameter["x"]] = await textReader.ReadToEndAsync();
                }
                return "<h1>u did it";
            };

            Get["/testview", true] = async (parameters, ct) =>
            {
                var persons = await DataConnection.Ask(x => x.GetPersonsAsync());
                return View["testy.cshtml", persons];
            };

            Get["/persons", true] = async (parameters, ct) =>
            {
                var persons = await DataConnection.Ask(x => x.GetPersonsAsync());
                return "<html><body><table>"
                    + string.Join("\n",
                        persons.OrderBy(item => item.Id)
                               .Select(item => "<tr><td>" + item.Id + "</td><td>"
                                               + item.Data.Name + "</td></tr>"))
                    + "</table></body></html>";
            };

            Post["/register_persons", true] = async (parameter, ct) =>
            {
                using (var textReader = new StreamReader(Request.Body))
                {
                    string data = await textReader.ReadToEndAsync();
                    var items = JsonConvert.DeserializeObject<List<PersonData>>(data);
                    await DataConnection.Ask(x => x.InsertPersonAsync(items));
                    return Nancy.HttpStatusCode.Created;
                }
            };
            /*
                +Authentication
            */
            Get["/login"] = parameter => EmptyPage; //Display an empty page on get, form itself will be implemented later.
            Get["/logout"] = parameter => NotImplementedPage; //No implementation yet.
            /*Post["/login", true] = async (parameters, ct) => //Post for login, chrome extension allows us to login. 
            {
                
                return "yay";
            };*/
            /*
                -Authentication
            */
        }
        public string NotImplementedPage
		{
            get
            {
                return @"<html><body><h1>Not implemented yet.</h1></body></html>";
            }
		}
        public string EmptyPage
        {
            get
            {
                return "";
            }
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

    //For testing purposes, we define our user class in this file, this needs moving.
    public class User : IUserIdentity
    {
        public string UserName
        {
            get; set;
        }
        public IEnumerable<string> Claims
        {
            get; set;
        }
    }
}