using System;
using AsyncPoco;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a location in the database.
	/// </summary>
	[PrimaryKey("id", autoIncrement = true)]
	public class Location : IEquatable<Location>
	{
		// AsyncPoco demands a parameterless constructor.
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
		/// Gets or sets the location's unique identifier.
		/// </summary>
		[Column("id")]
		[JsonProperty("id")]
		public int Id { get; private set; }

		/// <summary>
		/// Gets or sets the location's name.
		/// </summary>
		[Column("name")]
		[JsonProperty("name")]
		public string Name 
		{ 
			get { return Data.Name; } 
			private set { this.Data = new LocationData(value); }
		}

		/// <summary>
		/// Gets the location's data.
		/// </summary>
		[Ignore]
		[JsonProperty("data")]
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
