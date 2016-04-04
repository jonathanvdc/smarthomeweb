using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;
using System.Threading.Tasks;

namespace SmartHomeWeb.Modules.API
{
    public class ApiDayAverageModule : AggregatedMeasurementModuleBase
    {
        public ApiDayAverageModule() : base("api/day-average")
        { }

        public override TimeSpan TimeQuantum
        {
            get { return TimeSpan.FromDays(1); }
        }

        public override Task<Measurement> GetAggregatedAsync(DataConnection Connection, int SensorId, DateTime Time)
        {
            return Connection.GetDayAverageAsync(SensorId, Time);
        }
    }
}
