using System;
using AsyncPoco;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a person in the database.
	/// </summary>
	[PrimaryKey("id", autoIncrement = true)]
	public class Person
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
	}
}
