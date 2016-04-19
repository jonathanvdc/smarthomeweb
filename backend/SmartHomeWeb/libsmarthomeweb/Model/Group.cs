using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
    public sealed class Group : IEquatable<Group>
    {
        [JsonProperty("groupID")]
        public long Id
        {
            get; 
            set; 
        }
        [JsonProperty("groupName")]
        public string Name
        {
            get; 
            private set; 
        }
        [JsonProperty("groupMembers")]
        public List<Person> MemberList
        {
            get;
            set;

        }

        [JsonProperty("groupDescription")]
        public string Description
        {
            get; 
            private set;
        }

        public Group(long id, string name, string description, List<Person> members = null)
        {
            Id = id;
            Name = name;
            Description = description;
            MemberList = members ?? new List<Person>();

        }
        public void InsertMember(Person person)
        {
            MemberList.Add(person);
        }
        public bool Equals(Group other)
        {
            return Id == other.Id;
        }


    }
}