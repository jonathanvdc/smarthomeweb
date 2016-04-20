using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A data structure that contains a location's data, 
	/// but does not capture its unique identifier.
	/// </summary>
	public sealed class LocationData
	{
		[JsonConstructor]
		private LocationData()
		{ }

		public LocationData(string Name, Guid OwnerGuid, double? ElectricityPrice)
		{
			this.Name = Name;
            this.OwnerGuid = OwnerGuid;
			this.ElectricityPrice = ElectricityPrice;
		}

		/// <summary>
		/// Gets the location's name.
		/// </summary>
		[JsonProperty("name", Required = Required.Always)]
		public string Name { get; private set; }

		/// <summary>
		/// Gets the owner's globally unique identifier.
		/// </summary>
		[JsonIgnore]
		public Guid OwnerGuid { get; private set; }

        /// <summary>
        /// Gets the owner's GUID string.
        /// </summary>
		[JsonProperty("ownerGuid", Required = Required.Always)]
        public string OwnerGuidString
        {
            get { return OwnerGuid.ToString(); }
			private set { OwnerGuid = new Guid(value); }
        }

		/// <summary>
		/// Gets the electricity price for all sensors at this location,
		/// in some currency per kilowatt hour. This is entirely optional.
		/// </summary>
		[JsonProperty("electricityPrice")]
		public double? ElectricityPrice { get; private set; }
	}
}

