using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using SmartHomeWeb.Model;

namespace SmartHomeWeb.Modules
{
    public class SecureModule : NancyModule
    {
        public SecureModule(IFindUserMapper userMapper) : base("secure")
        {
            this.RequiresAuthentication();

            Get["/"] = parameters => SecuredPage;

            Get["/test"] = parameters => "test?";
        }

        private string SecuredPage => @"
            <html>
                <body>
                    <center><h1>You have accessed the secure portion of our site, "
                        + Context.CurrentUser.UserName + @"!</h1></center>
                </body>
            </html>";
    }
}
