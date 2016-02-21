using System;
using AsyncPoco;

namespace SmartHomeWeb
{
	[PrimaryKey("personId,locationId", autoIncrement = false)]
	public class HasLocation
	{
		public HasLocation()
		{
		}

		[Column("personId")]
		public string PersonId { get; private set; }

		[Column("locationId")]
		public string LocationId { get; private set; }
	}
}

