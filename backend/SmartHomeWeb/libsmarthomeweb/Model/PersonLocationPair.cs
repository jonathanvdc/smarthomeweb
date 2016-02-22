using System;
using AsyncPoco;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a person-location pair in the
	/// database. This can be used to tie a person to a
	/// specific location.
	/// </summary>
	public class PersonLocationPair
	{
		public PersonLocationPair()
		{
		}

		/// <summary>
		/// Gets or sets the person's unique identifier.
		/// </summary>
		[Column("personId")]
		[JsonProperty("personId")]
		public int PersonId { get; set; }

		/// <summary>
		/// Gets or sets the location's unique identifier.
		/// </summary>
		[Column("locationId")]
		[JsonProperty("locationId")]
		public int LocationId { get; set; }
	}
}
