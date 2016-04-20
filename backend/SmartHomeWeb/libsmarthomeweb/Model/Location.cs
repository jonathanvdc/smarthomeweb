using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a location in the database.
	/// </summary>
	public sealed class Location : IEquatable<Location>
	{
		// Serialization demands a parameterless constructor.
		[JsonConstructor]
		private Location()
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="SmartHomeWeb.Model.Location"/> class.
		/// </summary>
		/// <param name="Id">The location's unique identifier.</param>
		/// <param name="Data">A data structure that describes the location.</param>
		public Location(int Id, LocationData Data)
		{ 
			this.Id = Id;
			this.Data = Data;
		}

		/// <summary>
		/// Gets the location's unique identifier.
		/// </summary>
		[JsonProperty("id", Required = Required.Always)]
		public int Id { get; private set; }

		/// <summary>
		/// Gets the location's data.
		/// </summary>
		[JsonProperty("data", Required = Required.Always)]
		public LocationData Data { get; private set; }

		public bool Equals(Location Other)
		{
			return Id == Other.Id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is Location && Equals((Location)obj);
		}
	}
}
