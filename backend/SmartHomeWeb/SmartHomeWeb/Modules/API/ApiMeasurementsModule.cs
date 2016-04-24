﻿using System.Collections.Generic;
using SmartHomeWeb.Model;
using System;

namespace SmartHomeWeb.Modules.API
{
    public class ApiMeasurementsModule : ApiModule
    {
        public ApiMeasurementsModule() : base("api/measurements")
        {
            ApiGet("/", (_, dc) => dc.GetMeasurementsAsync());
			ApiGet("/{id}", (p, dc) => dc.GetMeasurementsAsync((int)p["id"]));
            ApiGet("/{id}/{timestamp}", (p, dc) => dc.GetMeasurementAsync((int)p["id"], (DateTime)p["timestamp"]));
			ApiGet("/{id}/{starttime}/{endtime}", (p, dc) => dc.GetMeasurementsAsync((int)p["id"], (DateTime)p["starttime"], (DateTime)p["endtime"]));

            ApiPost<List<Measurement>, object>("/", (_, items, dc) => dc.InsertMeasurementAsync(items));

            ApiPut<Measurement, object>("/updatetag", (_, item, dc) => dc.UpdateMeasurementTagsAsync(item));
        }
    }
}
