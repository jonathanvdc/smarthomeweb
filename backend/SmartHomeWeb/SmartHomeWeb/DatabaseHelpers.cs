using System;
using SmartHomeWeb.Model;
using System.Data;

namespace SmartHomeWeb
{
	public static class DatabaseHelpers
	{
		/// <summary>
		/// Reads a person entity from the given record.
		/// </summary>
		public static Person ReadPerson(IDataRecord Record)
		{
			return new Person(Record.GetInt32(0), new PersonData(Record.GetString(1)));
		}

		/// <summary>
		/// Reads a location entity from the given record.
		/// </summary>
		public static Location ReadLocation(IDataRecord Record)
		{
			return new Location(Record.GetInt32(0), new LocationData(Record.GetString(1)));
		}

		/// <summary>
		/// Reads a sensor entity from the given record.
		/// </summary>
		public static Sensor ReadSensor(IDataRecord Record)
		{
			return new Sensor(
				Record.GetInt32(0), 
				new SensorData(
					Record.GetString(1), Record.GetString(2), 
					Record.GetString(3), Record.GetInt32(4)));
		}

		/// <summary>
		/// Reads a person-location pair from the given record.
		/// </summary>
		/// <returns>The person-location pair.</returns>
		public static PersonLocationPair ReadPersonLocationPair(IDataRecord Record)
		{
			return new PersonLocationPair(Record.GetInt32(0), Record.GetInt32(1));
		}

		/// <summary>
		/// Quotes a string.
		/// </summary>
		public static string EncodeString(string Data)
		{
			return "'" + Data + "'";
		}

		/// <summary>
		/// Encodes a person's data as a SQL tuple.
		/// </summary>
		public static string EncodePersonData(PersonData Data)
		{
			return EncodeString(Data.Name);
		}
	}
}

