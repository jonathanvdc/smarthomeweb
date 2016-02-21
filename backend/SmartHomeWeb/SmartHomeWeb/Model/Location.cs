﻿using System;
using AsyncPoco;

namespace SmartHomeWeb.Model
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
