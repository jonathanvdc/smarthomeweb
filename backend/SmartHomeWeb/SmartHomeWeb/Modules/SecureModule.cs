using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Security;
using Nancy.Authentication.Forms;
using Nancy;
using SmartHomeWeb.Model;

namespace SmartHomeWeb.Modules
{
    public class UserMapper : IUserMapper
    {
        public UserMapper()
        {
            UserIdentities = new Dictionary<Guid, UserIdentity>();
        }

        public Dictionary<Guid, UserIdentity> UserIdentities { get; }

        public IUserIdentity GetUserFromIdentifier(Guid id, NancyContext context) => UserIdentities[id];

        public bool FindUser(string userName, string password, out UserIdentity userIdentity)
        {
            foreach (var u in UserIdentities)
            {
                if (u.Value.UserName == userName && u.Value.Password == password)
                {
                    userIdentity = u.Value;
                    return true;
                }
            }
            userIdentity = null;
            return false;
        }

        public void AddUser(Person person)
        {
            var u = new UserIdentity(person);
            UserIdentities.Add(u.Guid, u);
        }
    }

    public class UserIdentity : IUserIdentity
    {
        private readonly Person Person;

        public string UserName => Person.Data.UserName;
        public string Password => Person.Data.Password;
        public readonly Guid Guid;
        public IEnumerable<string> Claims => Enumerable.Empty<string>();
        
        public UserIdentity(Person person)
        {
            Person = person;

            // Given a person with ID 0x12345678, store the GUID 00000000-0000-0000-0000000012345678.
            byte[] bytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(Person.Id), 0, bytes, 12, 4);
            Guid = new Guid(bytes);
        }
    }
    
    public class SecureModule : NancyModule
    {
        private static UserMapper UM = new UserMapper();

        public SecureModule() : base("secure")
        {
            FormsAuthentication.Enable(this,
                new FormsAuthenticationConfiguration {
                    RedirectUrl = "~/login",
                    UserMapper = UM
                });

            Get["/"] = parameters =>
            {
                Console.WriteLine("a");
                var s = SecuredPage;
                Console.WriteLine("b");
                return s;
            };

            var persons = DataConnection.Ask(dc => dc.GetPersonsAsync()).Result;
            foreach (var p in persons)
            {
                UM.AddUser(p);
            }
        }

        public static bool FindUser(string userName, string password, out UserIdentity userIdentity)
            => UM.FindUser(userName, password, out userIdentity);

        private string SecuredPage => @"
            <html>
                <body>
                    <center><h1>You have accessed the secure portion of our site, "
                        + Context.CurrentUser.UserName + @"!</h1></center>
                </body>
            </html>";
    }
}
