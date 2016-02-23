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
		// AsyncPoco demands a parameterless constructor.
		private Sensor()
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="SmartHomeWeb.Model.Sensor"/> class.
		/// </summary>
		/// <param name="Id">The sensor's unique identifier.</param>
		/// <param name="Data">A data structure that describes the sensor.</param>
		public Sensor(int Id, SensorData Data)
		{
			this.Id = Id;
			this.Data = Data;
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
		[JsonIgnore]
		public string Name
		{ 
			get { return Data.Name; } 
			private set { this.Data = new SensorData(value, LocationId); }
		}

		/// <summary>
		/// Gets or sets the unique identifier 
		/// of this sensor's location.
		/// </summary>
		[Column("locationId")]
		[JsonIgnore]
		public int LocationId 
		{ 
			get { return Data.LocationId; } 
			private set { this.Data = new SensorData(Name, value); }
		}

		/// <summary>
		/// Gets this sensor's data.
		/// </summary>
		[Ignore]
		[JsonProperty("data")]
		public SensorData Data { get; private set; }

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

