using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SmartHomeWeb.Model;
using Nancy;

namespace SmartHomeWeb.Modules
{
    public class SmartHomeWebModule : Nancy.NancyModule
    {
       
        public static string PlatDuJour = "Yep. The server is running";
        public SmartHomeWebModule()
        {
            StaticConfiguration.EnableHeadRouting = true;

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

            Get["/login"] = parameter => SmartHomeWebModule.LoginPage; //Displays a simple login page
            Post["/login"] = parameter => //Process the post on /login
            {
                string name = Request.Form.username;
                string pass = Request.Form.password;
                User user;
                bool userFound = SecureModule.FindUser(name, pass, out user);

                if (userFound)
                {
                    return this.LoginAndRedirect(user.id, System.DateTime.Now.AddYears(1), "/");
                }
                else
                {
                    return Response.AsRedirect("/nopass");
                }
            };
            Get["/logout"] = parameter => SmartHomeWebModule.ComingSoonPage; //No implementation yet.
            Get["/nopass"] = parameter => SmartHomeWebModule.NotAuthorizedPage; //self explanatory.
            

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

        }
        public static string ErrorPage
        {
            get
            {
                return @"
                <html>
                    <body>
                        <h1>Something went horribly, horribly wrong. Our code monkeys are working on the problem.</h1>
                    </body>
                </html>
                ";
            }
        }
        public static string NotAuthorizedPage
        {
            get
            {
                return @"
                <html>
                    <body>
                        <h1>You shall not pass.</h1>
                    </body>
                </html>";
            }
        }
        public static string ComingSoonPage
		{
            get
            {
                return @"
                <html>
                    <body>
                        <h1>Not implemented yet, our engineers are working very hard to provide this page for you.</h1>
                    </body>
                </html>";
            }
		}
        public static string EmptyPage
        {
            get
            {
                return "";
            }
        }
        public static string IndexPage
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
        public static string LoginPage
        {
            get
            {
                return @"
                <html>
                    <body>
                        <form method=""post"" action="""">
                            Username:<br>
                            <input type=""text"" name=""username""><br>
                            Password:<br>
                            <input type=""text"" name=""password""><br><br>
                            <input type=""submit"" value=""Submit"">
                        </form>
                    </body>
                </html>
                ";
            }

        }

        private static Dictionary<string, string> Pages = new Dictionary<string, string>();
    }
    
}