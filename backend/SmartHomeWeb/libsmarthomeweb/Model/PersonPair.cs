using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
    /// <summary>
    /// A class that describes a person-person pair in
    /// the database.
    /// </summary>
    public sealed class PersonPair
    {
        private PersonPair()
        {
        }

        public PersonPair(Guid PersonOneGuid, Guid PersonTwoGuid)
        {
            this.PersonOneGuid = PersonOneGuid;
            this.PersonTwoGuid = PersonTwoGuid;
        }

        /// <summary>
        /// Gets the first person's GUID string.
        /// </summary>
        [JsonProperty("personOneGuid")]
        public string PersonOneGuidString
        {
            get { return PersonOneGuid.ToString(); }
            private set { PersonOneGuid = new Guid(value); }
        }

        /// <summary>
        /// Gets the first person's globally unique identifier.
        /// </summary>
        [JsonIgnore]
        public Guid PersonOneGuid { get; private set; }


        /// <summary>
        /// Gets the first person's GUID string.
        /// </summary>
        [JsonProperty("personTwoGuid")]
        public string PersonTwoGuidString
        {
            get { return PersonTwoGuid.ToString(); }
            private set { PersonTwoGuid = new Guid(value); }
        }

        /// <summary>
        /// Gets the first person's globally unique identifier.
        /// </summary>
        [JsonIgnore]
        public Guid PersonTwoGuid { get; private set; }

    }
}
