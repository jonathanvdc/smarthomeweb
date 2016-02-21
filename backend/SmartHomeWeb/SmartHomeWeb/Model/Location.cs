using System;
using AsyncPoco;

namespace SmartHomeWeb
{
	[PrimaryKey("id", autoIncrement = true)]
	public class Location
	{
		public Location()
		{ }

		[Column("id")]
		public int Id { get; set; }

		[Column("name")]
		public string Name { get; set; }
	}
}

