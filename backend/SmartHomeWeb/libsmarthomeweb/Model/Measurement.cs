﻿using Newtonsoft.Json;
using System;

namespace SmartHomeWeb.Model
{
    /// <summary>
    /// A data structure that describes a sensor's measurement.
    /// </summary>
    public sealed class Measurement
    {
        // Private constructor for serialization purposes.
		[JsonConstructor]
		private Measurement()
        { }

        public Measurement(
            int SensorId, DateTime Time, 
            double? MeasuredData, string Notes)
        {
            this.SensorId = SensorId;
            this.Time = Time;
            this.MeasuredData = MeasuredData;
            this.Notes = Notes;
        }

        /// <summary>
        /// Gets this identifier of the sensor 
        /// this measurement belongs to.
        /// </summary>
        [JsonProperty("sensorId", Required = Required.Always)]
        public int SensorId { get; private set; }

        /// <summary>
        /// Gets the time at which this measurement was made.
        /// </summary>
        [JsonProperty("timestamp", Required = Required.Always)]
        public DateTime Time { get; private set; }

        /// <summary>
        /// Gets the measurement's data.
        /// </summary>
        [JsonProperty("measurement", Required = Required.AllowNull)]
        public double? MeasuredData { get; private set; }
 
        /// <summary>
        /// Gets an optional note or remark that relates
        /// to this measurement. 
        /// </summary>
        [JsonProperty("notes")]
        public string Notes { get; private set; }

        public long ToJavascriptTimestamp()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var time = this.Time.Subtract(new TimeSpan(epoch.Ticks));
            return (long)(time.Ticks / 10000);
        }

		public override string ToString()
		{
			return string.Format("[Measurement: SensorId={0}, Time={1}, MeasuredData={2}, Notes={3}]", SensorId, Time, MeasuredData, Notes);
		}
    }
}

