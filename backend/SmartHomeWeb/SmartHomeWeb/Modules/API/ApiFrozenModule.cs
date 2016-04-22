using System;

namespace SmartHomeWeb.Modules.API
{
	public class ApiFrozenModule : ApiModule
	{
		public ApiFrozenModule() : base("/frozen")
		{
			ApiGet("/{timestamp}", (p, dc) => dc.IsFrozenAsync((DateTime)p["timestamp"]));

			Put["/{start}/{end}", true] = Ask((p, dc) => dc.FreezeAsync((DateTime)p["start"], (DateTime)p["end"]));
		}
	}
}

