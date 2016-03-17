using System;
using System.Data;
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

        public static double GetFloat64(IDataRecord Record, string Name)
        {
            return Record.GetDouble(Record.GetOrdinal(Name));
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
                new Guid(GetString(Record, "guid")), 
                new PersonData(
                    GetString(Record, "username"), GetString(Record, "password"),
                    GetString(Record, "name"), GetDateTime(Record, "birthdate"),
                    GetString(Record, "address"), GetString(Record, "city"), 
                    GetString(Record, "zipcode")));
        }

        /// <summary>
        /// Reads a message entity from the given record.
        /// </summary>
        public static Message ReadMessage(IDataRecord Record)
        {
            return new Message(
                GetInt32(Record, "id"),
                new MessageData(
                    GetInt32(Record, "sender"), GetInt32(Record, "recipient"),
                    GetString(Record, "message")));
        }

        /// <summary>
        /// Reads a measurement entity from the given record.
        /// </summary>
        public static Measurement ReadMeasurement(IDataRecord Record)
        {
            return new Measurement(
                GetInt32(Record, "sensorId"), GetDateTime(Record, "unixtime"),
                GetFloat64(Record, "measured"), GetString(Record, "notes"));
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
    }
}

