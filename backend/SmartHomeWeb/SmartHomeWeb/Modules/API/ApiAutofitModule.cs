using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
	/// <summary>
	/// An API module that automatically selects the 
	/// right level of granularity.
	/// </summary>
	public class ApiAutofitModule : ApiModule
	{
		public ApiAutofitModule() : base("api/autofit")
		{
			ApiGet("/{id}/{starttime}/{endtime}/{maxCount}", (p, dc) => GetFittedMeasurements(dc, (int)p["id"], (DateTime)p["starttime"], (DateTime)p["endtime"], (int)p["maxCount"]));
		}

		private static Task<IEnumerable<Measurement>> GetFittedMeasurements(
			DataConnection Connection, int SensorId, DateTime StartTime, DateTime EndTime,
			int MaxMeasurementCount)
		{
			if (EndTime < StartTime)
				throw new ArgumentException($"{nameof(StartTime)} was greater than {nameof(EndTime)}");

			var timeSpan = EndTime - StartTime;
			if (timeSpan.TotalMinutes <= MaxMeasurementCount)
			{
				// Assume that one measurement was made per minute.
				// This assumption may not _always_ prove to be correct,
				// but we'd like to keep database chatter to a minimum.
				return Connection.GetMeasurementsAsync(SensorId, StartTime, EndTime);
			}
			else if (timeSpan.TotalHours <= MaxMeasurementCount)
			{
				return Connection.GetHourAveragesAsync(
					SensorId, MeasurementAggregation.Quantize(StartTime, TimeSpan.FromHours(1)), 
					(int)Math.Ceiling(timeSpan.TotalHours));
			}
			else if (timeSpan.TotalDays <= MaxMeasurementCount)
			{
				return Connection.GetDayAveragesAsync(
					SensorId, MeasurementAggregation.Quantize(StartTime, TimeSpan.FromDays(1)), 
					(int)Math.Ceiling(timeSpan.TotalDays));
			}
			else
			{
				int yearCount = EndTime.Year - StartTime.Year;
				int monthCount = 12 * yearCount + EndTime.Month - StartTime.Month;
				if (monthCount <= MaxMeasurementCount)
				{
					return Connection.GetMonthAveragesAsync(
						SensorId, MeasurementAggregation.QuantizeMonth(StartTime), monthCount);
				}
				else
				{
					return Connection.GetYearAveragesAsync(
						SensorId, MeasurementAggregation.QuantizeYear(StartTime), yearCount);
				}
			}
		}
	}
}
