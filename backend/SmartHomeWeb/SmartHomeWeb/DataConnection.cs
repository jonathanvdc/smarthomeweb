using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Mono.Data.Sqlite;
using SmartHomeWeb.Model;
using System.Text;

namespace SmartHomeWeb
{

    public class DataConnection : IDisposable
    {
        public const string PersonTableName = "Person";
        public const string LocationTableName = "Location";
        public const string HasLocationTableName = "HasLocation";
        public const string SensorTableName = "Sensor";
        public const string MessageTableName = "Message";
        public const string MeasurementTableName = "Measurement";
        public const string FriendsTableName = "Friends";
        public const string HourAverageTableName = "HourAverage";

        // TODO: put this in some kind of configuration file.
        private const string ConnectionString = "Data Source=backend/database/smarthomeweb.db";

        private readonly SqliteConnection sqlite;

        private DataConnection()
        {
            sqlite = new SqliteConnection(ConnectionString);
        }

        /// <summary>
        /// Asynchronously creates a database connection.
        /// </summary>
        public static async Task<DataConnection> CreateAsync()
        {
            var result = new DataConnection();
            await result.sqlite.OpenAsync();
            return result;
        }

        /// <summary>
        /// Creates a task that executes an SqliteCommand, and interprets the results.
        /// </summary>
        /// <param name="Command">The command to execute.</param>
        /// <param name="ReadTuple">A function that reads a single tuple.</param>
        public async Task<IEnumerable<T>> ExecuteCommandAsync<T>(
            SqliteCommand Command,
            Func<IDataRecord, T> ReadTuple)
        {
            var results = new List<T>();
            using (var reader = await Command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    results.Add(ReadTuple(reader));
                }
            }
            return results;
        }

        /// <summary>
        /// Like <see cref="ExecuteCommandAsync{T}"/>, but expects a single result.
        /// If no result at all is found, the result is <value>null</value>.
        /// </summary>
        /// <param name="Command">The command to execute.</param>
        /// <param name="ReadTuple">A function that reads a single tuple.</param>
        public async Task<T> ExecuteCommandSingleAsync<T>(
            SqliteCommand Command,
            Func<IDataRecord, T> ReadTuple)
        {
            var result = await ExecuteCommandAsync(Command, ReadTuple);
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Creates a task that eagerly fetches all elements from
        /// the table with the given name.
        /// </summary>
        /// <param name="TableName">The table to fetch items from.</param>
        /// <param name="ReadTuple">A function that reads a single tuple.</param>
        public Task<IEnumerable<T>> GetTableAsync<T>(
            string TableName,
            Func<IDataRecord, T> ReadTuple)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM " + TableName;
                cmd.ExecuteNonQueryAsync();
                return ExecuteCommandAsync(cmd, ReadTuple);
            }
        }

        /// <summary>
        /// Creates a task that eagerly fetches all elements from
        /// the table with the given name. The given key function is
        /// then used to construct a dictionary that maps these keys
        /// to the items they belong to.
        /// </summary>
        /// <param name="TableName">The table to fetch items from.</param>
        /// <param name="GetKey">A function that extracts primary keys from tuples.</param>
        public async Task<IReadOnlyDictionary<TKey, TItem>> GetTableMapAsync<TItem, TKey>(
            string TableName,
            Func<IDataRecord, TItem> ReadTuple,
            Func<TItem, TKey> GetKey)
        {
            var items = await GetTableAsync(TableName, ReadTuple);
            return items.ToDictionary(GetKey);
        }

        /// <summary>
        /// Creates a task that fetches all persons in the database.
        /// </summary>
        public Task<IEnumerable<Person>> GetPersonsAsync()
        {
            return GetTableAsync(PersonTableName, DatabaseHelpers.ReadPerson);
        }

        /// <summary>
        /// Creates a task that fetches all locations in the database.
        /// </summary>
        public Task<IEnumerable<Location>> GetLocationsAsync()
        {
            return GetTableAsync(LocationTableName, DatabaseHelpers.ReadLocation);
        }

        /// <summary>
        /// Creates a task that fetches all person-location pairs
        /// from the has-location table.
        /// </summary>
        public Task<IEnumerable<PersonLocationPair>> GetHasLocationPairsAsync()
        {
            return GetTableAsync(HasLocationTableName, DatabaseHelpers.ReadPersonLocationPair);
        }

        /// <summary>
        /// Creates a task that fetches all sensors in the database.
        /// </summary>
        public Task<IEnumerable<Sensor>> GetSensorsAsync()
        {
            return GetTableAsync(SensorTableName, DatabaseHelpers.ReadSensor);
        }

        /// <summary>
        /// Creates a task that fetches all messages in the database.
        /// </summary>
        public Task<IEnumerable<Message>> GetMessagesAsync()
        {
            return GetTableAsync(MessageTableName, DatabaseHelpers.ReadMessage);
        }

        /// <summary>
        /// Creates a task that fetches all measurements in the database.
        /// </summary>
        public Task<IEnumerable<Measurement>> GetMeasurementsAsync()
        {
            return GetTableAsync(MeasurementTableName, DatabaseHelpers.ReadMeasurement);
        }

        /// <summary>
        /// Creates a task that fetches all friend pairs in the database.
        /// </summary>
        public Task<IEnumerable<PersonPair>> GetFriendsAsync()
        {
            return GetTableAsync(FriendsTableName, DatabaseHelpers.ReadPersonPair);
        }

        /// <summary>
        /// Creates a task that eagerly fetches all friend persons
        /// that have been added by the person with the given GUID.
        /// </summary>
        public Task<IEnumerable<Person>> GetFriendsAsync(Guid PersonGuid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT friend2.guid, friend2.username, friend2.name, friend2.password, 
                         friend2.birthdate, friend2.address, friend2.city, friend2.zipcode
                  FROM Friends as pair, Person as friend2
                  WHERE pair.personOne = @guid AND pair.personTwo = friend2.guid";
                cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());

                return ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
            }
        }

        /// <summary>
        /// Actually computes the the hour average for the 
        /// given sensor during the given hour.
        /// </summary>
        private async Task<Measurement> ComputeHourAverageAsync(int SensorId, DateTime Hour)
        {
            IEnumerable<Measurement> measurements;
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT *
                    FROM Measurement
                    WHERE Measurement.sensorId = @id AND Measurement.unixtime >= @starttime AND Measurement.unixtime < @endtime";
                cmd.Parameters.AddWithValue("@id", SensorId);
                cmd.Parameters.AddWithValue("@starttime", DatabaseHelpers.CreateUnixTimeStamp(Hour));
                cmd.Parameters.AddWithValue("@endtime", DatabaseHelpers.CreateUnixTimeStamp(Hour.AddHours(1)));
                measurements = await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMeasurement);
            }

            return MeasurementAggregation.Average(measurements, Hour);
        }

        /// <summary>
        /// Creates a task that fetches or computes the hour average for the 
        /// given sensor during the given hour.
        /// </summary>
        public async Task<Measurement> GetHourAverageAsync(int SensorId, DateTime Hour)
        {
            // Maybe we have already computed the hour average?
            // Try to fetch it from the database to find out.
            Measurement[] results;
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT *
                    FROM HourAverage
                    WHERE HourAverage.sensorId = @id AND HourAverage.unixtime = @unixtime";
                cmd.Parameters.AddWithValue("@id", SensorId);
                cmd.Parameters.AddWithValue("@unixtime", DatabaseHelpers.CreateUnixTimeStamp(Hour));

                results = (await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMeasurement)).ToArray();
            }

            if (results.Length > 0)
            {
                // Success. Return that precomputed 
                // hour average,
                return results[0];
            }
            else
            {
                // Bummer. Looks like we'll have to
                // compute the hour average, and insert
                // it into the database.
                var result = await ComputeHourAverageAsync(SensorId, Hour);
                await InsertMeasurementAsync(result, HourAverageTableName);
                return result;
            }
        }

        /// <summary>
        /// Creates a task that fetches a single row from the database, by
        /// looking up all of its keys.
        /// </summary>
        public Task<TItem> GetSingleByKeyAsync<TItem>(
            string TableName,
            IReadOnlyDictionary<string, object> KeyValues,
            Func<IDataRecord, TItem> ReadTuple)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                // Write SQL code for 
                //    SELECT * FROM {TableName} WHERE k1 = @v1 AND k2 = @v2 AND ... AND kn = @vn LIMIT 1

                var sql = new StringBuilder();
                sql.Append($"SELECT * FROM {TableName} WHERE ");
                int i = 0;
                foreach (var pair in KeyValues)
                {
                    if (i > 0)
                        sql.Append("AND ");
                    sql.Append(pair.Key);
                    sql.Append(" = ");
                    sql.Append("@v" + i);
                    cmd.Parameters.AddWithValue("@v" + i, pair.Value);
                    sql.Append(" ");
                    i++;
                }
                sql.Append("LIMIT 1");

                cmd.CommandText = sql.ToString();
                return ExecuteCommandSingleAsync(cmd, ReadTuple);
            }
        }

        /// <summary>
        /// Creates a task that fetches a single row from the database, by
        /// looking up one of its keys.
        /// </summary>
        public Task<TItem> GetSingleByKeyAsync<TKey, TItem>(
            string TableName,
            string KeyName,
            TKey KeyValue,
            Func<IDataRecord, TItem> ReadTuple)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM {TableName} WHERE {KeyName} = @v LIMIT 1";
                cmd.Parameters.AddWithValue("@v", KeyValue);
                return ExecuteCommandSingleAsync(cmd, ReadTuple);
            }
        }

        /// <summary>
        /// Creates a task that fetches a single person from the database by their GUID.
        /// </summary>
        public Task<Person> GetPersonByGuidAsync(Guid id) =>
            GetSingleByKeyAsync(PersonTableName, "guid", id.ToString(), DatabaseHelpers.ReadPerson);

        /// <summary>
        /// Creates a task that fetches a single person from the database by their username.
        /// </summary>
        public Task<Person> GetPersonByUsernameAsync(string username) =>
            GetSingleByKeyAsync(PersonTableName, "username", username, DatabaseHelpers.ReadPerson);

        /// <summary>
        /// Creates a task that fetches a single location from the database.
        /// </summary>
        public Task<Location> GetLocationByIdAsync(int id) =>
            GetSingleByKeyAsync(LocationTableName, "id", id, DatabaseHelpers.ReadLocation);

        /// <summary>
        /// Creates a task that fetches a single sensor from the database.
        /// </summary>
        public Task<Sensor> GetSensorByIdAsync(int id) =>
            GetSingleByKeyAsync(SensorTableName, "id", id, DatabaseHelpers.ReadSensor);

        /// <summary>
        /// Creates a task that fetches a single message from the database.
        /// </summary>
        public Task<Message> GetMessageByIdAsync(int id) =>
            GetSingleByKeyAsync(MessageTableName, "id", id, DatabaseHelpers.ReadMessage);

        /// <summary>
        /// Creates a task that fetches a single measurement from the database.
        /// </summary>
        public Task<Measurement> GetMeasurementAsync(int sensorId, long timestamp)
        {
            var keys = new Dictionary<string, object>
            {
                ["sensorId"] = sensorId,
                ["unixtime"] = timestamp
            };
            return GetSingleByKeyAsync(MeasurementTableName, keys, DatabaseHelpers.ReadMeasurement);
        }

        /// <summary>
        /// Creates a task that fetches a single measurement from the database.
        /// </summary>
        public Task<Measurement> GetMeasurementAsync(int sensorId, DateTime timestamp)
        {
            return GetMeasurementAsync(sensorId, DatabaseHelpers.CreateUnixTimeStamp(timestamp));
        }

        /// <summary>
        /// Creates a dictionary maps persons to their associated locations.
        /// </summary>
        public async Task<IReadOnlyDictionary<Person, IReadOnlyList<Location>>> GetPersonToLocationsMapAsync()
        {
            // First, retrieve all locations, persons and person-location pairs.
            var locations = await GetTableMapAsync(LocationTableName, DatabaseHelpers.ReadLocation, l => l.Id);
            var persons = await GetTableMapAsync(PersonTableName, DatabaseHelpers.ReadPerson, p => p.Guid);
            var pairs = await GetHasLocationPairsAsync();

            // Then construct a dictionary.
            var results = new Dictionary<Person, IReadOnlyList<Location>>();
            foreach (var person in persons.Values)
            {
                // Create one location list per person.
                results[person] = new List<Location>();
            }
            foreach (var pair in pairs)
            {
                // Convert every person-location pair to a location list item.
                var list = (List<Location>)results[persons[pair.PersonGuid]];
                list.Add(locations[pair.LocationId]);
            }

            return results;
        }

        /// <summary>
        /// Creates a task that eagerly fetches all locations that are
        /// associated with the given person in the database.
        /// </summary>
        public Task<IEnumerable<Location>> GetLocationsForPersonAsync(Guid PersonGuid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT loc.id, loc.name
                  FROM HasLocation as hasLoc, Location as loc
                  WHERE loc.id = hasLoc.locationId AND hasLoc.personGuid = @guid";
                cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());

                return ExecuteCommandAsync(cmd, DatabaseHelpers.ReadLocation);
            }
        }

        /// <summary>
        /// Creates a task that eagerly fetches all locations that are
        /// associated with the given person in the database.
        /// </summary>
        public Task<IEnumerable<Location>> GetLocationsForPersonAsync(Person Item)
           => GetLocationsForPersonAsync(Item.Guid);

        /// <summary>
        /// Gets the persons at the given location identifier.
        /// </summary>
        public Task<IEnumerable<Person>> GetPersonsAtLocationAsync(int LocationId)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT pers.guid, pers.username, pers.name, pers.password, pers.birthdate, pers.address, pers.city, pers.zipcode
                  FROM HasLocation as hasLoc, Person as pers
                  WHERE pers.guid = hasLoc.personGuid AND hasLoc.locationId = @locId";
                cmd.Parameters.AddWithValue("@locId", LocationId);
                return ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
            }
        }

        /// <summary>
        /// Gets all sensors at a given location
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        public Task<IEnumerable<Sensor>> GetSensorsAtLocation(Location loc)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT *
                  FROM Sensor
                  WHERE locationid = @locId";
                cmd.Parameters.AddWithValue("@locId", loc.Id);
                return ExecuteCommandAsync(cmd, DatabaseHelpers.ReadSensor);
            }

        }

        /// <summary>
        /// Gets all measurements for a given sensor
        /// </summary>
        /// <param name="sensor"></param>
        /// <returns></returns>
        public Task<IEnumerable<Measurement>> GetMeasurementsFromSensorAsync(Sensor sensor)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT *
                  FROM Measurement
                  WHERE sensorId = @senid";
                cmd.Parameters.AddWithValue("@senid", sensor.Id);
                return ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMeasurement);
            }
        }

        /// <summary>
        /// Gets all measurements for a sensor with given id
        /// </summary>
        /// <param name="sensor"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Measurement>> GetMeasurementsFromSensorAsync(int sensorid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                Sensor sensor = await GetSensorByIdAsync(sensorid);
                return await GetMeasurementsFromSensorAsync(sensor);
            }
        }

        /// <summary>
        /// Inserts the given person data into the Persons table.
        /// </summary>
        /// <param name="Data">The person data to insert into the table.</param>
        public Task InsertPersonAsync(PersonData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {PersonTableName}(guid, username, name, password, birthdate, address, city, zipcode) " +
                    "VALUES (@guid, @username, @name, @password, @birthdate, @address, @city, @zipcode)";
                cmd.Parameters.AddWithValue("@guid", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@username", Data.UserName);
                cmd.Parameters.AddWithValue("@name", Data.Name);
                cmd.Parameters.AddWithValue("@password", Data.Password);
                cmd.Parameters.AddWithValue("@birthdate", DatabaseHelpers.CreateUnixTimeStamp(Data.Birthdate));
                cmd.Parameters.AddWithValue("@address", Data.Address);
                cmd.Parameters.AddWithValue("@city", Data.City);
                cmd.Parameters.AddWithValue("@zipcode", Data.ZipCode);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all person data in the given list into the Persons table.
        /// </summary>
        /// <param name="Data">The list of person data to insert into the table.</param>
        public Task InsertPersonAsync(IEnumerable<PersonData> Data)
        {
            return Task.WhenAll(Data.Select(InsertPersonAsync));
        }

        /// <summary>
        /// Inserts the given location data into the Locations table.
        /// </summary>
        /// <param name="Data">The location data to insert into the table.</param>
        public Task InsertLocationAsync(LocationData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {LocationTableName}(name, owner) " +
                    "VALUES (@name, @owner)";
                cmd.Parameters.AddWithValue("@name", Data.Name);
                cmd.Parameters.AddWithValue("@owner", Data.OwnerGuidString);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all location data in the given list into the Locations table.
        /// </summary>
        /// <param name="Data">The list of location data to insert into the table.</param>
        public Task InsertLocationAsync(IEnumerable<LocationData> Data)
        {
            return Task.WhenAll(Data.Select(InsertLocationAsync));
        }

        /// <summary>
        /// Inserts the given sensor data into the Sensor table.
        /// </summary>
        /// <param name="Data">The sensor data to insert into the table.</param>
        public Task InsertSensorAsync(SensorData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {SensorTableName}(locationid, title, description, notes) " +
                    "VALUES (@locationid, @title, @description, @notes)";
                cmd.Parameters.AddWithValue("@locationid", Data.LocationId);
                cmd.Parameters.AddWithValue("@title", Data.Name);
                cmd.Parameters.AddWithValue("@description", Data.Description);
                cmd.Parameters.AddWithValue("@notes", Data.Notes);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all sensor data in the given list into the Sensor table.
        /// </summary>
        /// <param name="Data">The list of sensor data to insert into the table.</param>
        public Task InsertSensorAsync(IEnumerable<SensorData> Data)
        {
            return Task.WhenAll(Data.Select(InsertSensorAsync));
        }

        /// <summary>
        /// Inserts the given measurement into the table with the given name.
        /// </summary>
        /// <param name="Data">The measurement to insert into the table.</param>
        public Task InsertMeasurementAsync(Measurement Data, string TableName)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {TableName}(sensorId, unixtime, measured, notes) " +
                    "VALUES (@sensorId, @unixtime, @measured, @notes)";
                cmd.Parameters.AddWithValue("@sensorId", Data.SensorId);
                cmd.Parameters.AddWithValue("@unixtime", DatabaseHelpers.CreateUnixTimeStamp(Data.Time));
                cmd.Parameters.AddWithValue("@measured", Data.MeasuredData);
                cmd.Parameters.AddWithValue("@notes", Data.Notes);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts the given measurement into the Measurement table.
        /// </summary>
        /// <param name="Data">The measurement to insert into the table.</param>
        public Task InsertMeasurementAsync(Measurement Data)
        {
            return InsertMeasurementAsync(Data, MeasurementTableName);
        }

        /// <summary>
        /// Inserts all measurements in the given list into the Measurement table.
        /// </summary>
        /// <param name="Data">The list of measurements to insert into the table.</param>
        public Task InsertMeasurementAsync(IEnumerable<Measurement> Data)
        {
            return Task.WhenAll(Data.Select(InsertMeasurementAsync));
        }

        /// <summary>
        /// Inserts the given message  into the Message table.
        /// </summary>
        /// <param name="Data">The message to insert into the table.</param>
        public Task InsertMessageAsync(MessageData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {MessageTableName}(sender, recipient, message) " +
                    "VALUES (@usender, @recipient, @message)";
                cmd.Parameters.AddWithValue("@usender", Data.SenderGuid.ToString());
                cmd.Parameters.AddWithValue("@recipient", Data.RecipientGuid.ToString());
                cmd.Parameters.AddWithValue("@message", Data.Message);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all messages in the given list into the Message table.
        /// </summary>
        /// <param name="Data">The list of messages to insert into the table.</param>
        public Task InsertMessageAsync(IEnumerable<MessageData> Data)
        {
            return Task.WhenAll(Data.Select(InsertMessageAsync));
        }

        /// <summary>
        /// Inserts the given person-location pair into the
        /// HasLocation table. 
        /// </summary>
        /// <param name="Data">The person-location pair to insert into the table.</param>
        public Task InsertHasLocationPairAsync(PersonLocationPair Pair)
        {            
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {HasLocationTableName}(personGuid, locationId) " +
                    "VALUES (@personGuid, @locationId)";
                cmd.Parameters.AddWithValue("@personGuid", Pair.PersonGuidString);
                cmd.Parameters.AddWithValue("@locationId", Pair.LocationId);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all person-location pairs in the given list into the HasLocation table.
        /// </summary>
        /// <param name="Data">The list of person-location pairs to insert into the table.</param>
        public Task InsertHasLocationPairAsync(IEnumerable<PersonLocationPair> Data)
        {
            return Task.WhenAll(Data.Select(InsertHasLocationPairAsync));
        }

        /// <summary>
        /// Inserts a pair of friends in the Friends table.
        /// </summary>
        public Task InsertFriendsPairAsync(PersonPair Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {FriendsTableName}(personOne, personTwo) " +
                    "VALUES (@personOne, @personTwo)";
                cmd.Parameters.AddWithValue("@personOne", Data.PersonOneGuidString);
                cmd.Parameters.AddWithValue("@personTwo", Data.PersonTwoGuidString);
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all person-person pairs in the given list into the Friends table.
        /// </summary>
        /// <param name="Data">The list of person-person pairs to insert into the table.</param>
        public Task InsertFriendsPairAsync(IEnumerable<PersonPair> Data)
        {
            return Task.WhenAll(Data.Select(InsertFriendsPairAsync));
        }

        /// <summary>
        /// Close the database connection.
        /// </summary>
        public void Dispose() => sqlite.Close();

        /// <summary>
        /// Open a database connection, perform a single operation, and close it,
        /// asynchronously retrieving the result.
        /// </summary>
        public static async Task<T> Ask<T>(Func<DataConnection, Task<T>> operation)
        {
            using (var dc = await CreateAsync())
                return await operation(dc);
        }

        /// <summary>
        /// Open a database connection, perform a single operation, and close it.
        /// </summary>
        public static async Task Ask(Func<DataConnection, Task> operation)
        {
            using (var dc = await CreateAsync())
                await operation(dc);
        }
    }
}
