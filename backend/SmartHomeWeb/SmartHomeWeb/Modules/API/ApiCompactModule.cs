using System;

namespace SmartHomeWeb.Modules.API
{
	public class ApiCompactModule : ApiModule
	{
		public ApiCompactModule() : base("api/compact")
		{
			// PUT, because compaction is idempotent
			Put["/measurements/{start}/{end}", true] = Ask((p, dc) => dc.CompactAsync((DateTime)p["start"], (DateTime)p["end"]), "WAL", "NORMAL");
		}
	}
}

