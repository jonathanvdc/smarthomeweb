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
        public LocationData(string Name, Guid OwnerGuid)
		{
			this.Name = Name;
            this.OwnerGuid = OwnerGuid;
		}

		/// <summary>
		/// Gets the location's name.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; private set; }

        /// <summary>
        /// Gets the owner's GUID string.
        /// </summary>
        [JsonProperty("ownerGuid")]
        public string OwnerGuidString
        {
            get { return OwnerGuid.ToString(); }
            private set { OwnerGuid = new Guid(value); }
        }

        /// <summary>
        /// Gets the owner's globally unique identifier.
        /// </summary>
        [JsonIgnore]
        public Guid OwnerGuid { get; private set; }
	}
}

