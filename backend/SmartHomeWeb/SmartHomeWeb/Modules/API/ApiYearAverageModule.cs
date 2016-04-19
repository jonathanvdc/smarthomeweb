using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
	public class ApiYearAverageModule : AggregatedMeasurementModuleBase
	{
		public ApiYearAverageModule() : base("api/year-average")
		{ }

		/// <summary>
		/// Quantizes the given date-time.
		/// </summary>
		public override DateTime Quantize(DateTime Time)
		{
			return new DateTime(Time.Year, 1, 1, 0, 0, 0, 0, Time.Kind);
		}

		/// <summary>
		/// Adds a single quantum of time to this date-time.
		/// </summary>
		public override DateTime AddQuantum(DateTime Time)
		{
			return Time.AddYears(1);
		}

		/// <summary>
		/// Gets the aggregated measurement for the sensor with the given
		/// ID, at the given quantized time.
		/// </summary>
		public override Task<Measurement> GetAggregatedAsync(DataConnection Connection, int SensorId, DateTime Time)
		{
			return Connection.GetYearAverageAsync(SensorId, Time);
		}
	}
}
