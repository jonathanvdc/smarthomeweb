using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A data structure that contains a location's data, 
	/// but does not capture its unique identifier.
	/// </summary>
	public class LocationData
	{
		public LocationData(string Name)
		{
			this.Name = Name;
		}

		/// <summary>
		/// Gets the location's name.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; private set; }
	}
}

