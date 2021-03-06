﻿using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A class that describes a sensor in the database.
	/// </summary>
	public sealed class Sensor : IEquatable<Sensor>
	{
		// Serialization demands a parameterless constructor.
		[JsonConstructor]
		private Sensor()
		{
			// Initialize this to keep the deserializer from running
			// into trouble.
			this.Data = new SensorData(null, null, null, 0);
		}

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
		/// Gets the sensor's unique identifier.
		/// </summary>
        [JsonProperty("id", Required = Required.Always)]
		public int Id { get; set; }

		/// <summary>
		/// Gets this sensor's data.
		/// </summary>
        [JsonProperty("data", Required = Required.Always)]
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

