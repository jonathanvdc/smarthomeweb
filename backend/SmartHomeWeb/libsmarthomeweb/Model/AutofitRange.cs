using System;
using Newtonsoft.Json;

namespace SmartHomeWeb.Model
{
    /// <summary>
    /// Defines an input structure for the autofitting
    /// algorithm: a bounded number of measurements for
    /// a given period of time. 
    /// </summary>
    public sealed class AutofitRange
    {
        [JsonConstructor]
        private AutofitRange()
        {
        }

        public AutofitRange(
            int SensorId, DateTime StartTime, 
            DateTime EndTime, int MaxMeasurements)
        {
            this.SensorId = SensorId;
            this.StartTime = StartTime;
            this.EndTime = EndTime;
            this.MaxMeasurements = MaxMeasurements;
        }

        /// <summary>
        /// Gets the sensor's identifier.
        /// </summary>
        /// <value>The sensor's identifier.</value>
        [JsonProperty("sensorId", Required = Required.Always)]
        public int SensorId { get; private set; }

        /// <summary>
        /// Gets this autofitted range's start time.
        /// </summary>
        /// <value>The start time.</value>
        [JsonProperty("startTime", Required = Required.Always)]
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets this autofitted range's end time.
        /// </summary>
        /// <value>The end time.</value>
        [JsonProperty("endTime", Required = Required.Always)]
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// Gets the maximal number of measurements for this
        /// autofitted range.
        /// </summary>
        /// <value>The maximal number of measurements.</value>
        [JsonProperty("maxMeasurements", Required = Required.Always)]
        public int MaxMeasurements { get; private set; }

        /// <summary>
        /// Gets this autofitted range's duration.
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Gets a value indicating whether this range is empty.
        /// </summary>
        /// <value><c>true</c> if this range is empty; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsEmpty => FrozenPeriod.IsEmptyRange(Tuple.Create(StartTime, EndTime));

        public override string ToString()
        {
            return string.Format("[AutofitRange: SensorId={0}, StartTime={1}, EndTime={2}, MaxMeasurements={3}]", SensorId, StartTime, EndTime, MaxMeasurements);
        }
    }
}

