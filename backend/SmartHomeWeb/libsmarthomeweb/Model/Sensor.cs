using System;
using AsyncPoco;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a sensor in the database.
	/// </summary>
	[PrimaryKey("id", autoIncrement = true)]
	public class Sensor : IEquatable<Sensor>
	{
		public Sensor()
		{
		}

		/// <summary>
		/// Gets or sets the sensor's unique identifier.
		/// </summary>
		[Column("id")]
		[JsonProperty("id")]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the sensor's name.
		/// </summary>
		[Column("name")]
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier 
		/// of this sensor's location.
		/// </summary>
		[Column("locationId")]
		[JsonProperty("locationId")]
		public string LocationId { get; set; }

		public bool Equals(Sensor Other)
		{
			return Id == Other.Id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is Sensor && Equals((Sensor)obj);
		}
	}
}

