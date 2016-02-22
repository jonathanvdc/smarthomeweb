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
		public Person()
		{ }

		/// <summary>
		/// Gets or sets the person's unique identifier.
		/// </summary>
		[Column("id")]
		[JsonProperty("id")]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the person's name.
		/// </summary>
		[Column("name")]
		[JsonProperty("name")]
		public string Name { get; set; }

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
