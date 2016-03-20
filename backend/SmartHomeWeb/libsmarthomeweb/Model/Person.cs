using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a person in the database.
	/// </summary>
	public sealed class Person : IEquatable<Person>
	{
		// AsyncPoco demands a parameterless constructor.
		private Person()
		{ }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartHomeWeb.Model.Person"/> class.
        /// </summary>
        /// <param name="Guid">The person's globally unique identifier.</param>
        /// <param name="Data">A data structure that describes the person.</param>
        public Person(Guid Guid, PersonData Data)
		{ 
			this.Guid = Guid;
			this.Data = Data;
		}

        /// <summary>
        /// Gets the person's GUID string.
        /// </summary>
        [JsonProperty("personGuid")]
        public string GuidString
        {
            get { return Guid.ToString(); }
            private set { Guid = new Guid(value); }
        }

        /// <summary>
        /// Gets the person's globally unique identifier.
        /// </summary>
        [JsonIgnore]
        public Guid Guid { get; private set; }

		/// <summary>
		/// Gets the person's data.
		/// </summary>
		[JsonProperty("data")]
		public PersonData Data { get; private set; }

		public bool Equals(Person Other)
		{
			return Guid == Other.Guid;
		}

		public override int GetHashCode()
		{
			return Guid.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is Person && Equals((Person)obj);
		}
	}
}
