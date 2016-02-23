using System;
using Newtonsoft.Json;

namespace SmartHomeWeb
{
	/// <summary>
	/// A data structure that contains a sensor's data, 
	/// but does not capture their unique identifier.
	/// </summary>
	public class SensorData
	{
		public SensorData(string Name, int LocationId)
		{
			this.Name = Name;
			this.LocationId = LocationId;
		}

		/// <summary>
		/// Gets or sets the sensor's name.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; private set; }

		/// <summary>
		/// Gets or sets the unique identifier 
		/// of this sensor's location.
		/// </summary>
		[JsonProperty("locationId")]
		public int LocationId { get; private set; }
	}
}

