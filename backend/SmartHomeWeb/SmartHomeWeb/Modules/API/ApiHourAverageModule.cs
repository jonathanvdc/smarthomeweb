using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
    /// <summary>
    /// A base class that provides common functionality for
    /// modules that offer aggregated measurements.
    /// </summary>
    public abstract class AggregatedMeasurementModuleBase : ApiModule
    {
        public AggregatedMeasurementModuleBase(string Url) : base(Url)
        {
            ApiGet("/{id}/{timestamp}", (p, dc) => GetAggregatedAsync(dc, (int)p["id"], Quantize((DateTime)p["timestamp"])));
            ApiGet("/{id}/{timestamp}/{count}", (p, dc) => GetAggregatedRangeAsync(dc, (int)p["id"], Quantize((DateTime)p["timestamp"]), (int)p["count"]));
        }

        /// <summary>
        /// Quantizes the given date-time.
        /// </summary>
		public abstract DateTime Quantize(DateTime Time);

		/// <summary>
		/// Adds a single quantum of time to this date-time.
		/// </summary>
		public abstract DateTime AddQuantum(DateTime Time);

        /// <summary>
        /// Gets the aggregated measurement for the sensor with the given
        /// ID, at the given quantized time.
        /// </summary>
        public abstract Task<Measurement> GetAggregatedAsync(DataConnection Connection, int SensorId, DateTime Time);

        /// <summary>
        /// Gets a range of aggregated measurements for the sensor with the 
        /// given ID. The initial aggregated measurement is identified by the 
        /// given quantized start time, and further measurements are produced
        /// by incrementally adding the time quantum to this initial time. 
        /// </summary>
        public Task<IEnumerable<Measurement>> GetAggregatedRangeAsync(DataConnection Connection, int SensorId, DateTime StartTime, int ResultCount)
        {
            var results = new Task<Measurement>[ResultCount];
            var time = StartTime;
            for (int i = 0; i < ResultCount; i++)
            {
                results[i] = GetAggregatedAsync(Connection, SensorId, time);
				time = AddQuantum(time);
            }
            return Task.WhenAll(results).ContinueWith<IEnumerable<Measurement>>(t => t.Result);
        }
    }

	/// <summary>
	/// A base class that provides common functionality for
	/// modules that offer aggregated measurements, where
	/// the quantum of time is fixed.
	/// </summary>
	public abstract class FixedQuantumAggregateModuleBase : AggregatedMeasurementModuleBase
	{
		public FixedQuantumAggregateModuleBase(string Url) : base(Url)
		{ }

		/// <summary>
		/// Gets the quantum of time for this aggregated
		/// measurement module.
		/// </summary>
		/// <value>The time quantum.</value>
		public abstract TimeSpan TimeQuantum { get; }

		/// <summary>
		/// Quantizes the given date-time.
		/// </summary>
		public override DateTime Quantize(DateTime Time)
		{
			return MeasurementAggregation.Quantize(Time, TimeQuantum);
		}

		/// <summary>
		/// Adds a single quantum of time to this date-time.
		/// </summary>
		public override DateTime AddQuantum(DateTime Time)
		{
			return Time + TimeQuantum;
		}
	}

	public class ApiHourAverageModule : FixedQuantumAggregateModuleBase
    {
        public ApiHourAverageModule() : base("api/hour-average")
        { }

        public override TimeSpan TimeQuantum
        {
            get { return TimeSpan.FromHours(1); }
        }

        public override Task<Measurement> GetAggregatedAsync(DataConnection Connection, int SensorId, DateTime Time)
        {
            return Connection.GetHourAverageAsync(SensorId, Time);
        }
    }
}
