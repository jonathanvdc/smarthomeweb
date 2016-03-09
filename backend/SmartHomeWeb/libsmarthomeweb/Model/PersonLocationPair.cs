using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a person-location pair in the
	/// database. This can be used to tie a person to a
	/// specific location.
	/// </summary>
	public sealed class PersonLocationPair
	{
		private PersonLocationPair()
		{
		}

		public PersonLocationPair(int PersonId, int LocationId)
		{
			this.PersonId = PersonId;
			this.LocationId = LocationId;
		}

		/// <summary>
		/// Gets the person's unique identifier.
		/// </summary>
		[JsonProperty("personId")]
		public int PersonId { get; private set; }

		/// <summary>
		/// Gets the location's unique identifier.
		/// </summary>
		[JsonProperty("locationId")]
		public int LocationId { get; private set; }
	}
}
