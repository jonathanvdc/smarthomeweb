using System;
using AsyncPoco;

namespace SmartHomeWeb
{
	/// <summary>
	/// A class that describes a sensor in the database.
	/// </summary>
	[PrimaryKey("id", autoIncrement = true)]
	public class Sensor
	{
		public Sensor()
		{
		}

		/// <summary>
		/// Gets or sets the sensor's unique identifier.
		/// </summary>
		[Column("id")]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the sensor's name.
		/// </summary>
		[Column("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier 
		/// of this sensor's location.
		/// </summary>
		[Column("locationId")]
		public string LocationId { get; set; }
	}
}

