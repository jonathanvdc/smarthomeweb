using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SmartHomeWeb.Model;

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
            return (long)(Time - UnixEpoch).TotalSeconds;
        }

        public static string GetString(IDataRecord Record, string Name)
        {
            int fieldIndex = Record.GetOrdinal(Name);
            if (Record.IsDBNull(fieldIndex))
                return null;
            else
                return Record.GetString(fieldIndex);
        }

        public static int GetInt32(IDataRecord Record, string Name)
        {
            return Record.GetInt32(Record.GetOrdinal(Name));
        }

        public static long GetInt64(IDataRecord Record, string Name)
        {
            return Record.GetInt64(Record.GetOrdinal(Name));
        }

		public static bool GetBoolean(IDataRecord Record, string Name)
		{
			return Record.GetBoolean(Record.GetOrdinal(Name));
		}

        public static double GetFloat64(IDataRecord Record, string Name)
        {
            return Record.GetDouble(Record.GetOrdinal(Name));
        }

        public static double? GetFloat64OrNull(IDataRecord Record, string Name)
        {
            int index = Record.GetOrdinal(Name);
            return Record.IsDBNull(index) ? (double?)null : Record.GetDouble(index);
        }

        public static DateTime GetDateTime(IDataRecord Record, string Name)
        {
            return ParseUnixTimeStamp(GetInt64(Record, Name));
        }

        public static Guid GetGuid(IDataRecord Record, string Name)
        {
            return new Guid(GetString(Record, Name));
        }

        /// <summary>
        /// Assume we joined message, person, person to get a full representation of the record.
        /// Offset the 4 Message fields, then 9 fields for each person we've done previously.
        /// Ie, 0 or 9.
        /// Then get by column number, because the columns retain identical names in this representation, for some bizarre reason.
        /// </summary>
        public static Person ReadPerson(IDataRecord Record, int index)
        {
            var offset = 10 + index * 9;
            return new Person(Record.GetGuid(offset), 
                new PersonData(
                    Record.GetString(offset + 1), Record.GetString(offset + 3),
                    Record.GetString(offset + 2), ParseUnixTimeStamp(Record.GetInt64(offset + 4)),
                    Record.GetString(offset + 5), Record.GetString(offset + 6),
                    Record.GetString(offset + 7), Record.GetBoolean(offset + 8)));

                

        }
        /// <summary>
        /// Reads a person entity from the given record.
        /// </summary>
        public static Person ReadPerson(IDataRecord Record)
        {
            return new Person(
                GetGuid(Record, "guid"),
                new PersonData(
                    GetString(Record,"username"), GetString(Record, "password"),
                    GetString(Record, "name"), GetDateTime(Record, "birthdate"),
                    GetString(Record, "address"), GetString(Record, "city"),
                    GetString(Record, "zipcode"), GetBoolean(Record, "isAdmin")));
        }
        /// <summary>
        /// Reads a group entity from the given record.
        /// </summary>
        public static Group ReadGroup(IDataRecord Record)
        {
            return new Group(
                GetInt32(Record, "id"),
                GetString(Record, "name"),
                GetString(Record, "description")
            );
        }

        /// <summary>
        /// Reads a message entity from the given record.
        /// </summary>
        public static Message ReadMessage(IDataRecord Record)
        {
            return new Message(
                GetInt32(Record, "id"),
                new MessageData(
                    GetGuid(Record, "sender"), GetGuid(Record, "recipient"),
                    GetString(Record, "message")));
        }

        /// <summary>
        /// Reads a measurement entity from the given record.
        /// </summary>
        public static Measurement ReadMeasurement(IDataRecord Record)
        {
            return new Measurement(
                GetInt32(Record, "sensorId"), GetDateTime(Record, "unixtime"),
                GetFloat64OrNull(Record, "measured"), GetString(Record, "notes"));
        }

        /// <summary>
        /// Reads a location entity from the given record.
        /// </summary>
        public static Location ReadLocation(IDataRecord Record)
        {
            return new Location(
                GetInt32(Record, "id"), 
                new LocationData(
                    GetString(Record, "name"),
                    GetGuid(Record, "owner"),
					GetFloat64OrNull(Record, "electricityPrice")));
        }

        /// <summary>
        /// Reads a sensor entity from the given record.
        /// </summary>
        public static Sensor ReadSensor(IDataRecord Record)
        {
            return new Sensor(
                GetInt32(Record, "id"), 
                new SensorData(
                    GetString(Record, "title"), GetString(Record, "description"), 
                    GetString(Record, "notes"), GetInt32(Record, "locationid")));
        }

        /// <summary>
        /// Reads a person-location pair from the given record.
        /// </summary>
        /// <returns>The person-location pair.</returns>
        public static PersonLocationPair ReadPersonLocationPair(IDataRecord Record)
        {
            return new PersonLocationPair(new Guid(Record.GetString(0)), Record.GetInt32(1));
        }

        /// <summary>
        /// Reads a person-person pair from the database.
        /// </summary>
        public static PersonPair ReadPersonPair(IDataRecord Record)
        {
            return new PersonPair(GetGuid(Record, "personOne"), GetGuid(Record, "personTwo"));
        }

		/// <summary>
		/// Reads a frozen period of time from the database.
		/// </summary>
		public static FrozenPeriod ReadFrozenPeriod(IDataRecord Record)
		{
			return new FrozenPeriod(
				GetDateTime(Record, "startTime"), 
				GetDateTime(Record, "endTime"), 
				(CompactionLevel)GetInt32(Record, "compactionLevel"));
		}

        public static Graph ReadGraph(IDataRecord record)
        {
            if (GetString(record, "graph") != null && GetString(record, "name") != null &&
                GetString(record, "owner") != null)
            {
                return new Graph(
                    GetInt32(record, "graphId"),
                    new GraphData(
                        GetString(record, "graph"),
                        GetString(record, "name"),
                        GetGuid(record, "owner")));
            }
            return null;

        }

        public static WallPost ReadWallPost(IDataRecord record)
        {
            return new WallPost(
                GetInt32(record, "id"),
                ReadPerson(record, 0),
                ReadPerson(record, 1),
                GetString(record, "message"),
                ReadGraph(record));
        }
    }
}

