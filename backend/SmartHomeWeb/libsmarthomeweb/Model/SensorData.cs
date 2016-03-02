using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A data structure that contains a sensor's data, 
	/// but does not capture their unique identifier.
	/// </summary>
	public class SensorData
	{
		public SensorData(string Name, string Description, string Notes, int LocationId)
		{
			this.Name = Name;
			this.LocationId = LocationId;
			this.Description = Description;
			this.Notes = Notes;
		}

		/// <summary>
		/// Gets the sensor's name.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; private set; }

		/// <summary>
		/// Gets the sensor's (optional) description.
		/// </summary>
		/// <value>The description.</value>
		[JsonProperty("description")]
		public string Description { get; private set; }

		/// <summary>
		/// Gets the sensor's (optional) notes.
		/// </summary>
		/// <value>The notes.</value>
		[JsonProperty("notes")]
		public string Notes { get; private set; }

		/// <summary>
		/// Gets the unique identifier 
		/// of this sensor's location.
		/// </summary>
		[JsonProperty("locationId")]
		public int LocationId { get; private set; }
	}
}

