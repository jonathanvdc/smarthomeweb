using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SmartHomeWeb.Model;
using Nancy.Security;
using Nancy.Authentication.Forms;
using Nancy;
using System;

namespace SmartHomeWeb.Modules
{
    /*
        +Authentication
    */
    public class UserMapper : IUserMapper
    {
        public List<User> users; 
        public IUserIdentity GetUserFromIdentifier(Guid id, NancyContext context)
        {
            //Example code, would need to fill in user list and implement Guid saving within User.
            IUserIdentity ret = null;
            foreach (User u in users)
            {
                if (u.id == id)
                {
                    ret = u;
                }
            }
            return ret;
        }
        public UserMapper()
        {
            users = new List<User>();
            users.Add(new User());
        }
    }
    //For testing purposes, we define our user class in this file, this needs moving as well as just better integration.
    public class User : IUserIdentity
    {
        public Guid id
        {
            get;
            private set;
        }
        public string UserName
        {
            get; 
            private set;
        }
        public string password
        {
            get;
            private set;
        }
        public IEnumerable<string> Claims
        {   
            get; 
            private set;
        }
        public User()
        {
            //Default construct a single user; for testing purposes.
            UserName = "admin";
            password = "admin";
            id = Guid.Parse("00000000000000000000000000000000");
        }

    }

    public class SecureModule : NancyModule
    {
        public static SecureModule theOne;
        private static UserMapper UM = new UserMapper();
        public static FormsAuthenticationConfiguration authenticationConfiguration = new FormsAuthenticationConfiguration() {
            RedirectUrl = "~/login",
            UserMapper = UM
        }; //UserMapper needs to be implemented decently, but works as an example.
        public SecureModule() : base("/secure")
        {
            theOne = this;
            FormsAuthentication.Enable(this, authenticationConfiguration); //Enables form auth.
            Get["/"] = parameters =>
            {
                this.RequiresAuthentication();
                return this.SecuredPage;
            };
        }
        public static SecureModule getRef()
        {
            if (theOne == null)
            {
                throw new System.Exception("Not working");
            }
            return theOne;
        }
        public bool FindUser(string name, string pass, out User user)
        {
            foreach (User u in UM.users)
            {
                if (u.UserName == name)
                {
                    if (u.password == pass)
                    {
                        user = u;
                        return true;
                    }
                }
            }
            user = null;
            return false;
            
            
        }
        public string SecuredPage
        {
            get
            {
                return @"
                    <html>
                        <body>
                            <center><h1>You have accessed the secure portion of our site, " + this.Context.CurrentUser.UserName + @"!</h1></center>
                        </body>
                    </html>";
            }
        }
        /*Post["/login", true] = async (parameters, ct) => //Post for login, chrome extension allows us to login. 
        {

            return "yay";
        };*/

    }
    /*
        -Authentication
    */
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
            /*
                +Authentication
            */
            Get["/login"] = parameter => SmartHomeWebModule.LoginPage; //Displays a simple login page
            Get["/logout"] = parameter => SmartHomeWebModule.ComingSoonPage; //No implementation yet.
            Get["/nopass"] = parameter => SmartHomeWebModule.NotAuthorizedPage;
            Post["/login"] = parameter =>
            {
                string redirectPath;
                string name = Request.Form.username;
                string pass = Request.Form.password;
                User user;
                bool userFound = SecureModule.getRef().FindUser(name, pass, out user);

                if (userFound)
                {
                    Context.CurrentUser = user;
                    redirectPath = "/secure";
                }
                else
                {
                    redirectPath = "/nopass";
                }
                return Response.AsRedirect(redirectPath);
            };
            /*
                -Authentication
            */
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
                        <form method=""post"">
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