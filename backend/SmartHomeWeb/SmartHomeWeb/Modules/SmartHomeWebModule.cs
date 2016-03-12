using System;
using Nancy;
using Nancy.Authentication.Forms;

namespace SmartHomeWeb.Modules
{
    public class SmartHomeWebModule : NancyModule
    {
        public SmartHomeWebModule()
        {
            // StaticConfiguration.EnableHeadRouting = true;

            Get["/", true] = async (parameters, ct) =>
            {
                var persons = await DataConnection.Ask(x => x.GetPersonsAsync());
                return View["home.cshtml", persons];
            };

            Get["/login"] = _ => View["login.cshtml"];

            Post["/login"] = parameter => //Process the post on /login
            {
                string name = Request.Form.username;
                string pass = Request.Form.password;
                UserIdentity user;

                return SecureModule.FindUser(name, pass, out user)
                    ? this.LoginAndRedirect(user.Guid, DateTime.Now.AddYears(1), "/")
                    : Response.AsRedirect("/nopass");
            };
            Get["/logout"] = parameter => ComingSoonPage;
            Get["/nopass"] = parameter => {
                Console.WriteLine("nopass");
                return NotAuthorizedPage;
            };
        }
        public static string ErrorPage => @"
                <html>
                    <body>
                        <h1>Something went horribly, horribly wrong. Our code monkeys are working on the problem.</h1>
                    </body>
                </html>";

        public static string NotAuthorizedPage => @"
                <html>
                    <body>
                        <h1>You shall not pass.</h1>
                    </body>
                </html>";

        public static string ComingSoonPage => @"
                <html>
                    <body>
                        <h1>Not implemented yet, our engineers are working very hard to provide this page for you.</h1>
                    </body>
                </html>";

        public static string EmptyPage => "";
    }    
}