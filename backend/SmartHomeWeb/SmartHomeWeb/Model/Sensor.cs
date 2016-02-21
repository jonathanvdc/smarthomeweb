using System;
using AsyncPoco;

namespace SmartHomeWeb
{
	[PrimaryKey("id", autoIncrement = true)]
	public class Sensor
	{
		public Sensor()
		{
		}

		[Column("id")]
		public int Id { get; set; }

		[Column("name")]
		public string Name { get; set; }

		[Column("locationId")]
		public string LocationId { get; set; }
	}
}

