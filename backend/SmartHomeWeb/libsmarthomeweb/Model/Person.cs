using System;
using AsyncPoco;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a person in the database.
	/// </summary>
	[PrimaryKey("id", autoIncrement = true)]
	public class Person : IEquatable<Person>
	{
		// AsyncPoco demands a parameterless constructor.
		private Person()
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="SmartHomeWeb.Model.Person"/> class.
		/// </summary>
		/// <param name="Id">The person's unique identifier.</param>
		/// <param name="Data">A data structure that describes the person.</param>
		public Person(int Id, PersonData Data)
		{ 
			this.Id = Id;
			this.Data = Data;
		}

		/// <summary>
		/// Gets the person's unique identifier.
		/// </summary>
		[Column("id")]
		[JsonProperty("id")]
		public int Id { get; private set; }

		/// <summary>
		/// Gets the person's data.
		/// </summary>
		[Ignore]
		[JsonProperty("data")]
		public PersonData Data { get; private set; }

		/// <summary>
		/// Gets the person's name.
		/// </summary>
		[Column("name")]
		[JsonIgnore]
		public string Name 
		{ 
			get { return Data.Name; } 
			private set { this.Data = new PersonData(value); }
		}

		public bool Equals(Person Other)
		{
			return Id == Other.Id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is Person && Equals((Person)obj);
		}
	}
}
