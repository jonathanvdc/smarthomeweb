using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
    public interface IFindUserMapper : IUserMapper
    {
        bool FindUser(string userName, string password, out UserIdentity userIdentity);
    }

    public class UserMapper : IFindUserMapper
    {
        public IUserIdentity GetUserFromIdentifier(Guid guid, NancyContext context)
        {
            var person = DataConnection.Ask(dc => dc.GetPersonByGuidAsync(guid)).Result;
            return person == null ? null : new UserIdentity(person);
        }

        public bool FindUser(string userName, string password, out UserIdentity userIdentity)
        {
            // TODO: make this a real query instead of fetching *all* users.
            var persons = DataConnection.Ask(dc => dc.GetPersonsAsync()).Result;

            foreach (var p in persons)
            {
                if (p.Data.UserName == userName && p.Data.Password == password)
                {
                    userIdentity = new UserIdentity(p);
                    return true;
                }
            }
            userIdentity = null;
            return false;
        }
    }

    public class UserIdentity : IUserIdentity
    {
        private readonly Person Person;

        public string UserName => Person.Data.UserName;
        public string Password => Person.Data.Password;
        public IEnumerable<string> Claims => Enumerable.Empty<string>();
        public Guid Guid => Person.Guid;

        public UserIdentity(Person person)
        {
            Person = person;
        }
    }
}