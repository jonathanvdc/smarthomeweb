using System;
using Nancy;

namespace SmartHomeWeb.Modules.API
{
	public class ApiCompactModule : ApiModule
	{
		public ApiCompactModule() : base("api/compact")
		{
			// PUT, because compaction is idempotent
			Put["/measurements/{start}/{end}", true] = Ask((p, dc) => dc.CompactAsync((DateTime)p["start"], (DateTime)p["end"]), "WAL", "NORMAL");
			Put["/hour-average/{start}/{end}", true] = Ask((p, dc) => dc.CompactAsync((DateTime)p["start"], (DateTime)p["end"], CompactionLevel.HourAverages), "WAL", "NORMAL");
			Put["/day-average/{start}/{end}", true] = Ask((p, dc) => dc.CompactAsync((DateTime)p["start"], (DateTime)p["end"], CompactionLevel.DayAverages), "WAL", "NORMAL");
			Put["/vacuum", true] = async (p, ct) =>
			{
				using (var dc = await DataConnection.CreateAsync("WAL", "NORMAL"))
				{
					await dc.VacuumAsync();
				}
				return HttpStatusCode.OK;
			};
		}
	}
}

