using System;
using System.Collections.Generic;
using Nancy.Security;
using Nancy.Authentication.Forms;
using Nancy;
namespace SmartHomeWeb.Modules
{
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
            id = Guid.Parse("10010010010010010010010010010010");
            Claims = new List<string>();
        }

    }

    public class SecureModule : NancyModule
    {
        private static UserMapper UM = new UserMapper();
        public static FormsAuthenticationConfiguration authenticationConfiguration = new FormsAuthenticationConfiguration() {
            RedirectUrl = "~/login",
            UserMapper = UM
        }; //UserMapper needs to be implemented decently, but works as an example.
        public SecureModule() : base("/secure")
        {
            FormsAuthentication.Enable(this, authenticationConfiguration); //Enables form auth.
            Get["/"] = parameters =>
            {
                //this.RequiresAuthentication();
                return this.SecuredPage;
            };
            
        }
        public static bool FindUser(string name, string pass, out User user)
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

    }
}
