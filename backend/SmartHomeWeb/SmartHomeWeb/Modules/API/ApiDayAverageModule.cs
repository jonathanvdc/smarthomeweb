using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
	public class ApiDayAverageModule : FixedQuantumAggregateModuleBase
    {
        public ApiDayAverageModule() : base("api/day-average")
        {
            ApiPut<Measurement, object>("/updatetag", (_, item, dc) => dc.UpdateMeasurementTagsAsync(item, "DayAverage"));
        }

        public override TimeSpan TimeQuantum
        {
            get { return TimeSpan.FromDays(1); }
        }

        public override Task<Measurement> GetAggregatedAsync(DataConnection Connection, int SensorId, DateTime Time)
        {
            return Connection.GetDayAverageAsync(SensorId, Time);
        }

		public override Task<IEnumerable<Measurement>> GetAggregatedRangeAsync(DataConnection Connection, int SensorId, DateTime StartTime, int ResultCount)
		{
			return Connection.GetDayAveragesAsync(SensorId, StartTime, ResultCount);
		}
    }
}
