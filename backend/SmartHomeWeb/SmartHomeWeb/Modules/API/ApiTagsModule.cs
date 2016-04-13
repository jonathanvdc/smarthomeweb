using System;
using System.Collections.Generic;

namespace SmartHomeWeb.Modules.API
{
	public class ApiTagsModule : ApiModule
	{
		public ApiTagsModule() : base("api/sensor-tags")
		{
			ApiGet("/{id}/", (p, dc) => dc.GetSensorTagsAsync((int)p["id"]));

			ApiPost<List<string>, object>("/{id}/", (p, items, dc) => dc.InsertSensorTagAsync((int)((dynamic)p)["id"], items));
		}
	}
}

