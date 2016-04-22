using System;

namespace SmartHomeWeb.Modules.API
{
	public class ApiFrozenModule : ApiModule
	{
		public ApiFrozenModule() : base("api/frozen")
		{
			ApiGet("/{timestamp}", (p, dc) => dc.IsFrozenAsync((DateTime)p["timestamp"]));

			// PUT, because freezing is idempotent
			Put["/{start}/{end}", true] = Ask((p, dc) => dc.FreezeAsync((DateTime)p["start"], (DateTime)p["end"]));

			// There is no DELETE/unfreeze request. This is by design, as freezing is supposed
			// to guarantee the database's integrity. Unfreezing would therefore destroy the 
			// database's integrity. We absolutely don't want that.
		}
	}
}

