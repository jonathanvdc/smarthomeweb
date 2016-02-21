using System;
using AsyncPoco;

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
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the location's name.
		/// </summary>
		[Column("name")]
		public string Name { get; set; }
	}
}
