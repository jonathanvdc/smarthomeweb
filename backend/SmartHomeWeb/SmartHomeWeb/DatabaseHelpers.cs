using System;
using SmartHomeWeb.Model;
using System.Data;

namespace SmartHomeWeb
{
	public static class DatabaseHelpers
	{
        private static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ParseUnixTimeStamp(long Timestamp)
        {
            return UnixEpoch.AddSeconds(Timestamp);
        }

        public static long CreateUnixTimeStamp(DateTime Time)
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }

        public static string GetString(IDataRecord Record, string Name)
        {
            return Record.GetString(Record.GetOrdinal(Name));
        }

        public static int GetInt32(IDataRecord Record, string Name)
        {
            return Record.GetInt32(Record.GetOrdinal(Name));
        }

        public static long GetInt64(IDataRecord Record, string Name)
        {
            return Record.GetInt64(Record.GetOrdinal(Name));
        }

        public static DateTime GetDateTime(IDataRecord Record, string Name)
        {
            return ParseUnixTimeStamp(GetInt64(Record, Name));
        }

		/// <summary>
		/// Reads a person entity from the given record.
		/// </summary>
		public static Person ReadPerson(IDataRecord Record)
		{
			return new Person(
                GetInt32(Record, "id"), 
                new PersonData(
                    GetString(Record, "username"), GetString(Record, "name"),
                    GetString(Record, "password"), GetDateTime(Record, "birthdate"),
                    GetString(Record, "address"), GetString(Record, "city"), 
                    GetString(Record, "zipcode")));
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
	}
}

