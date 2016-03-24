using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;

namespace SmartHomeWeb.Modules.API
{
    public class ApiMeasurementsModule : ApiModule
    {
        public ApiMeasurementsModule() : base("api/measurements")
        {
            ApiGet("/", (_, dc) => dc.GetMeasurementsAsync());
            ApiGet("/{id}", (p, dc) => dc.GetMeasurementsFromSensorAsync((int)p["id"]));
            ApiGet("/{id}/{timestamp}", (p, dc) => dc.GetMeasurementAsync((int)p["id"], (DateTime)p["timestamp"]));

            ApiPost<List<Measurement>, object>("/", (_, items, dc) => dc.InsertMeasurementAsync(items));
        }
    }
}
