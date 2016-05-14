using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
	public class ApiMonthAverageModule : AggregatedMeasurementModuleBase
	{
		public ApiMonthAverageModule() : base("api/month-average")
		{
            ApiPut<Measurement, object>("/updatetag", (_, item, dc) => 
                dc.UpdateMeasurementNotesAsync(item, DataConnection.MonthAverageTableName));
        }

		/// <summary>
		/// Quantizes the given date-time.
		/// </summary>
		public override DateTime Quantize(DateTime Time)
		{
			return MeasurementAggregation.QuantizeMonth(Time);
		}

		/// <summary>
		/// Adds a single quantum of time to this date-time.
		/// </summary>
		public override DateTime AddQuantum(DateTime Time)
		{
			return Time.AddMonths(1);
		}

		/// <summary>
		/// Gets the aggregated measurement for the sensor with the given
		/// ID, at the given quantized time.
		/// </summary>
		public override Task<Measurement> GetAggregatedAsync(DataConnection Connection, int SensorId, DateTime Time)
		{
			return Connection.GetMonthAverageAsync(SensorId, Time);
		}

		public override Task<IEnumerable<Measurement>> GetAggregatedRangeAsync(DataConnection Connection, int SensorId, DateTime StartTime, int ResultCount)
		{
			return Connection.GetMonthAveragesAsync(SensorId, StartTime, ResultCount);
		}
	}
}
