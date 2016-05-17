using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
	/// <summary>
	/// An API module that automatically selects the 
	/// right level of granularity.
	/// </summary>
	public abstract class ApiAutofitModuleBase : ApiModule
	{
	    protected ApiAutofitModuleBase(string Url, Predictivity Predictivity) : base(Url)
        {
            ApiGet("/{id}/{starttime}/{endtime}/{maxCount}", (p, dc) =>
                GetFittedMeasurements(
                    dc, (int)p["id"],
                    (DateTime)p["starttime"],
                    (DateTime)p["endtime"],
                    (int)p["maxCount"],
                    Predictivity));

            ApiGet("/total/{id}/{starttime}/{endtime}/{maxcount}", (p, dc) =>
                GetSumMeasurements(
                    dc, (int)p["id"],
                    (DateTime)p["starttime"],
                    (DateTime)p["endtime"],
                    (int)p["maxcount"],
                    Predictivity));
        }

        private static Task<IEnumerable<Measurement>> GetFittedMeasurements(
			DataConnection Connection, int SensorId, DateTime StartTime, DateTime EndTime,
			int MaxMeasurementCount, Predictivity Predictivity)
		{
			if (EndTime < StartTime)
				throw new ArgumentException($"{nameof(StartTime)} was greater than {nameof(EndTime)}");

			var timeSpan = EndTime - StartTime;
            var yearCount = EndTime.Year - StartTime.Year;
            var monthCount = 12 * yearCount + EndTime.Month - StartTime.Month;

            if (timeSpan.TotalMinutes <= MaxMeasurementCount)
			{
				// Assume that one measurement was made per minute.
				// This assumption may not _always_ prove to be correct,
				// but we'd like to keep database chatter to a minimum.
				return Connection.GetMeasurementsAsync(SensorId, StartTime, EndTime);
			}
		    if (timeSpan.TotalHours <= MaxMeasurementCount)
		    {
		        return Connection.GetHourAveragesAsync(
		            SensorId, MeasurementAggregation.Quantize(StartTime, TimeSpan.FromHours(1)), 
		            (int)Math.Round(timeSpan.TotalHours));
		    }
		    if (timeSpan.TotalDays <= MaxMeasurementCount)
		    {
		        return Connection.GetDayAveragesAsync(
		            SensorId, MeasurementAggregation.Quantize(StartTime, TimeSpan.FromDays(1)), 
		            (int)Math.Round(timeSpan.TotalDays));
		    }
		    if (monthCount <= MaxMeasurementCount)
		    {
		        return Connection.GetMonthAveragesAsync(
		            SensorId, MeasurementAggregation.QuantizeMonth(StartTime), monthCount);
		    }
		    return Connection.GetYearAveragesAsync(
		        SensorId, MeasurementAggregation.QuantizeYear(StartTime), yearCount);
		}

        private static async Task<double> GetSumMeasurements(
            DataConnection Connection, int SensorId, DateTime StartTime, DateTime EndTime,
            int MaxMeasurementCount, Predictivity Predictivity)
        {
            var measurements = await GetFittedMeasurements(
                Connection, SensorId, StartTime, EndTime, MaxMeasurementCount, Predictivity);

            return measurements
                .Where(x => x.MeasuredData != null)
                .Sum(x => x.MeasuredData.Value);
        }
	}

    public class ApiAutofitModule : ApiAutofitModuleBase
    {
        public ApiAutofitModule() : base("api/autofit", Predictivity.NonPredictive) { }
    }

    public class ApiPredictiveModule : ApiAutofitModuleBase
    {
        public ApiPredictiveModule() : base("api/predictive", Predictivity.Predictive) { }
    }
}
