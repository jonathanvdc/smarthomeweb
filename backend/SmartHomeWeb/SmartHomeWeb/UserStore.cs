using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            userIdentity = DataConnection.Ask(dc => dc.GetUserIdentityAsync(userName, password)).Result;
            return userIdentity != null;
        }
    }

    public class UserIdentity : IUserIdentity
    {
        private readonly Person Person;

        public string UserName => Person.Data.UserName;
        public string Password => Person.Data.Password;
        public Guid Guid => Person.Guid;

        public IEnumerable<string> Claims
        {
            get { if (Person.Data.IsAdministrator) yield return "admin"; }
        }

        public UserIdentity(Person person)
        {
            Person = person;
        }
    }
}