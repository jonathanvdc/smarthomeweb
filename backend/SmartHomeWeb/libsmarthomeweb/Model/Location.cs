using System;
using AsyncPoco;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a location in the database.
	/// </summary>
	[PrimaryKey("id", autoIncrement = true)]
	public class Location
	{
		public Location()
		{ }

		/// <summary>
		/// Gets or sets the location's unique identifier.
		/// </summary>
		[Column("id")]
		[JsonProperty("id")]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the location's name.
		/// </summary>
		[Column("name")]
		[JsonProperty("name")]
		public string Name { get; set; }
	}
}
