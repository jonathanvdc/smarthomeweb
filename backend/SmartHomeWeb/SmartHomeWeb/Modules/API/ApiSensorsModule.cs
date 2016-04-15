using System.Collections.Generic;
using System.IO;
using Nancy;
using Newtonsoft.Json;
using SmartHomeWeb.Model;
using System;

namespace SmartHomeWeb.Modules.API
{
    public class ApiSensorsModule : ApiModule
    {
        public ApiSensorsModule() : base("api/sensors")
        {
            ApiGet("/", (_, dc) => dc.GetSensorsAsync());
            ApiGet("/{id}/", (p, dc) => dc.GetSensorByIdAsync((int)p["id"]));
			ApiGet("/by-tag/{tag}", (p, dc) => dc.GetSensorsByTagAsync((string)p["tag"]));
			ApiGet("/at-location/{id}", (p, dc) => dc.GetSensorsAtLocationAsync((int)p["id"]));

            ApiPost<List<SensorData>, object>("/", (_, items, dc) => dc.InsertSensorAsync(items));
        }
    }
}
