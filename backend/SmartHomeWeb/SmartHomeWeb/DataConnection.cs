using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
        public const string TwoWayFriendsTableName = "TwoWayFriends";
        public const string PendingFriendRequestTableName = "PendingFriendRequest";
        public const string HourAverageTableName = "HourAverage";
        public const string DayAverageTableName = "DayAverage";
		public const string MonthAverageTableName = "MonthAverage";
		public const string YearAverageTableName = "YearAverage";
		public const string SensorTagTableName = "SensorTag";

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
        public async Task<IEnumerable<T>> GetTableAsync<T>(
            string TableName,
            Func<IDataRecord, T> ReadTuple)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM " + TableName;
				return await ExecuteCommandAsync(cmd, ReadTuple);
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
        /// Creates a task that fetches all sensors and their matching tags in the database.
        /// </summary>
        public async Task<IEnumerable<Tuple<Sensor, IEnumerable<string>>>> GetSensorTagsPairsAsync()
        {
            var sensors = await Ask(x => x.GetSensorsAsync());
            var items = new List<Tuple<Sensor, IEnumerable<string>>>();
            for (int i = 0; i < sensors.Count(); i++)
            {
                var sensor = sensors.ElementAt(i);
                var tags = await Ask(x => x.GetSensorTagsAsync(sensor.Id));
                items.Add(Tuple.Create(sensor, tags));
            }

            return items;
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
		/// Gets the state of the 'friends' relation between persons
		/// with the given GUIDs.
		/// </summary>
		public async Task<FriendsState> GetFriendsState(Guid PersonOneGuid, Guid PersonTwoGuid)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = @"
                  SELECT pair.personOne, pair.PersonTwo
                  FROM Friends as pair
                  WHERE (pair.personOne = @guid1 AND pair.personTwo = @guid2) 
                     OR (pair.personOne = @guid2 AND pair.personTwo = @guid1)";
				cmd.Parameters.AddWithValue("@guid1", PersonOneGuid.ToString());
				cmd.Parameters.AddWithValue("@guid2", PersonTwoGuid.ToString());

				var tuples = await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPersonPair);
				var result = FriendsState.None;
				foreach (var item in tuples)
				{
					if (item.PersonOneGuid == PersonOneGuid)
						result |= FriendsState.FriendRequestSent;
					else
						result |= FriendsState.FriendRequestRecieved;
				}
				return result;
			}
		}

        /// <summary>
        /// Creates a task that eagerly fetches all mutual friends
		/// of the person identified by the given GUID.
        /// </summary>
        public async Task<IEnumerable<Person>> GetFriendsAsync(Guid PersonGuid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT friend2.*
                  FROM TwoWayFriends as pair, Person as friend2
                  WHERE pair.personOne = @guid AND pair.personTwo = friend2.guid";
                cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());

				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
            }
        }

        /// <summary>
        /// Creates a task that eagerly fetches persons who sent friend requests
        /// *to* the person with the given globally unique identifier. 
        /// </summary>
        public async Task<IEnumerable<Person>> GetRecievedFriendRequestsAsync(Guid PersonGuid)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $@"
                  SELECT friend2.*
                  FROM {PendingFriendRequestTableName} as pair, Person as friend2
                  WHERE pair.personOne = friend2.guid AND pair.personTwo = @guid";
				cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());

				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
			}
		}

        /// <summary>
		/// Creates a task that eagerly fetches persons who were sent friend requests
		/// *by* the person with the given globally unique identifier. 
		/// </summary>
		public async Task<IEnumerable<Person>> GetSentFriendRequestsAsync(Guid PersonGuid)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = @"
                  SELECT friend2.*
                  FROM PendingFriendRequest as pair, Person as friend2
                  WHERE pair.personOne = @guid AND pair.personTwo = friend2.guid";
				cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());

				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
			}
		}

        /// <summary>
        /// Actually computes the hour average for the 
        /// given sensor during the given hour.
        /// </summary>
        private async Task<Measurement> ComputeHourAverageAsync(int SensorId, DateTime Hour)
        {
			var measurements = await GetMeasurementsAsync(SensorId, Hour, Hour.AddHours(1));
            return MeasurementAggregation.Aggregate(measurements, SensorId, Hour);
        }

        /// <summary>
        /// Actually computes the day average for the 
        /// given sensor during the given day.
        /// </summary>
        private async Task<Measurement> ComputeDayAverageAsync(int SensorId, DateTime Day)
        {
			var cache = new AggregationCache(this, SensorId, Day, Day.AddDays(1));

			await cache.PrefetchHourAveragesAsync();
			var result = await cache.GetDayAverageAsync(Day);
			await cache.FlushHoursAsync();
			return result;
        }

		/// <summary>
		/// Actually computes the month average for the 
		/// given sensor during the given month.
		/// </summary>
		private async Task<Measurement> ComputeMonthAverageAsync(int SensorId, DateTime Month)
		{
			var cache = new AggregationCache(this, SensorId, Month, Month.AddMonths(1));

			await cache.PrefetchDayAveragesAsync();
			var result = await cache.GetMonthAverageAsync(Month);
			await cache.FlushDaysAsync();
			return result;
		}

		/// <summary>
		/// Actually computes the month average for the 
		/// given sensor during the given month.
		/// </summary>
		private async Task<Measurement> ComputeYearAverageAsync(int SensorId, DateTime Year)
		{
			var end = Year.AddYears(1);

			// Create one task per day, and have each task
			// fetch a day average.
			var tasks = new List<Task<Measurement>>();

			var month = Year;
			while (month < end)
			{
				tasks.Add(GetMonthAverageAsync(SensorId, month));
				month = month.AddMonths(1);
			}

			var measurements = await Task.WhenAll(tasks);

			// Just use the mean to aggregate data here, because we have already removed
			// outliers when computing the hour average.
			return MeasurementAggregation.Aggregate(measurements, SensorId, Year, Enumerable.Average);
		}

        /// <summary>
        /// Creates a task that fetches or computes the average measurement for the 
        /// given sensor at the given time. A cache table name and an average 
        /// computation function are also given.
        /// </summary>
        private async Task<Measurement> GetAverageAsync(
            int SensorId, DateTime Time, string TableName, 
            Func<int, DateTime, Task<Measurement>> ComputeAverageAsync)
        {
            // Perhaps we have already computed the average.
            // Try to fetch it from the database to find out.
            Measurement[] results;
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM {TableName} as m " +
                    "WHERE m.sensorId = @id AND m.unixtime = @unixtime";
                cmd.Parameters.AddWithValue("@id", SensorId);
                cmd.Parameters.AddWithValue("@unixtime", DatabaseHelpers.CreateUnixTimeStamp(Time));

                results = (await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMeasurement)).ToArray();
            }

            if (results.Length > 0)
            {
                // Success. Return that precomputed average,
                return results[0];
            }
            else
            {
                // Bummer. Looks like we'll have to
                // compute the average, and insert
                // it into the database.
                var result = await ComputeAverageAsync(SensorId, Time);
                await InsertMeasurementAsync(result, TableName);
                return result;
            }
        }

        /// <summary>
        /// Creates a task that fetches or computes the hour average for the 
        /// given sensor during the given hour.
        /// </summary>
        public Task<Measurement> GetHourAverageAsync(int SensorId, DateTime Hour)
        {
            return GetAverageAsync(SensorId, Hour, HourAverageTableName, ComputeHourAverageAsync);
        }

        /// <summary>
        /// Creates a task that fetches or computes the day average for the 
        /// given sensor during the given day.
        /// </summary>
        public Task<Measurement> GetDayAverageAsync(int SensorId, DateTime Day)
        {
            return GetAverageAsync(SensorId, Day, DayAverageTableName, ComputeDayAverageAsync);
        }

		/// <summary>
		/// Creates a task that fetches or computes the month average for the 
		/// given sensor during the given month.
		/// </summary>
		public Task<Measurement> GetMonthAverageAsync(int SensorId, DateTime Month)
		{
			return GetAverageAsync(SensorId, Month, MonthAverageTableName, ComputeMonthAverageAsync);
		}


		/// <summary>
		/// Creates a task that fetches or computes the year average for the 
		/// given sensor during the given year.
		/// </summary>
		public Task<Measurement> GetYearAverageAsync(int SensorId, DateTime Year)
		{
			return GetAverageAsync(SensorId, Year, YearAverageTableName, ComputeYearAverageAsync);
		}

        /// <summary>
        /// Creates a task that fetches a single row from the database, by
        /// looking up all of its keys.
        /// </summary>
        public async Task<TItem> GetSingleByKeyAsync<TItem>(
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
				return await ExecuteCommandSingleAsync(cmd, ReadTuple);
            }
        }

        /// <summary>
        /// Creates a task that fetches a single row from the database, by
        /// looking up one of its keys.
        /// </summary>
        public async Task<TItem> GetSingleByKeyAsync<TKey, TItem>(
            string TableName,
            string KeyName,
            TKey KeyValue,
            Func<IDataRecord, TItem> ReadTuple)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM {TableName} WHERE {KeyName} = @v LIMIT 1";
                cmd.Parameters.AddWithValue("@v", KeyValue);
				return await ExecuteCommandSingleAsync(cmd, ReadTuple);
            }
        }

		/// <summary>
		/// Asynchronously performs the given action in a single transaction.
		/// </summary>
		public async Task PerformTransaction(Func<DataConnection, Task> Transaction)
		{
			using (var trans = sqlite.BeginTransaction())
			{
				try
				{
					await Transaction(this);
				}
				catch (Exception)
				{
					// If something goes wrong, we'll roll back the
					// transaction and re-throw the exception.
					trans.Rollback();
					throw;
				}
				// If everything went fine, then we'll commit
				// the transaction.
				trans.Commit();
			}
		}

		/// <summary>
		/// Asynchronously performs the given action in a single transaction,
		/// and returns the result.
		/// </summary>
		public async Task<T> PerformTransaction<T>(Func<DataConnection, Task<T>> Transaction)
		{
			T result;
			using (var trans = sqlite.BeginTransaction())
			{
				try
				{
					result = await Transaction(this);
				}
				catch (Exception)
				{
					// If something goes wrong, we'll roll back the
					// transaction and re-throw the exception.
					trans.Rollback();
					throw;
				}
				// If everything went fine, then we'll commit
				// the transaction.
				trans.Commit();
			}
			return result;
		}

		/// <summary>
		/// Inserts all tuples in the given list into the database.
		/// </summary>
		public Task InsertManyAsync<T>(IEnumerable<T> Data, Func<T, Task> InsertItem)
		{
			return Task.WhenAll(Data.Select(InsertItem));
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
        /// Creates a task that looks up a username/password pair in the database, and returns
        /// a corresponding UserIdentity, if it exists. Otherwise, `null` is returned.
        /// </summary>
        public async Task<UserIdentity> GetUserIdentityAsync(string userName, string password)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $@"
                  SELECT * FROM {PersonTableName} as p
                  WHERE p.username = @userName AND p.password = @password";
                cmd.Parameters.AddWithValue("@userName", userName);
                cmd.Parameters.AddWithValue("@password", password);

                var results = await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
                var person = results.FirstOrDefault();

                return person == null ? null : new UserIdentity(person);
            }
        }

        /// <summary>
        /// Creates a task that fetches a single location from the database.
        /// </summary>
        public Task<Location> GetLocationByIdAsync(int id) =>
            GetSingleByKeyAsync(LocationTableName, "id", id, DatabaseHelpers.ReadLocation);

		/// <summary>
		/// Creates a task that fetches a single location from the database.
		/// </summary>
		public Task<Location> GetLocationByNameAsync(string Name) =>
			GetSingleByKeyAsync(LocationTableName, "name", Name, DatabaseHelpers.ReadLocation);

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
        public async Task<IEnumerable<Location>> GetLocationsForPersonAsync(Guid PersonGuid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT loc.id, loc.name, loc.owner, loc.electricityPrice
                  FROM HasLocation as hasLoc, Location as loc
                  WHERE loc.id = hasLoc.locationId AND hasLoc.personGuid = @guid";
                cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());

				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadLocation);
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
		public async Task<IEnumerable<Person>> GetPersonsAtLocationAsync(int LocationId)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT pers.guid, pers.username, pers.name, pers.password, pers.birthdate, pers.address, pers.city, pers.zipcode, pers.isAdmin
                  FROM HasLocation as hasLoc, Person as pers
                  WHERE pers.guid = hasLoc.personGuid AND hasLoc.locationId = @locId";
                cmd.Parameters.AddWithValue("@locId", LocationId);
				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
            }
        }


		/// <summary>
		/// Gets all sensors at a given location
		/// </summary>
		/// <param name="loc"></param>
		/// <returns></returns>
		public Task<IEnumerable<Sensor>> GetSensorsAtLocationAsync(Location loc)
		{
			return GetSensorsAtLocationAsync(loc.Id);
		}

        /// <summary>
        /// Gets all sensors at the location with the given identifier.
        /// </summary>
        /// <param name="LocationId">The location's unique identifier.</param>
        /// <returns></returns>
		public async Task<IEnumerable<Sensor>> GetSensorsAtLocationAsync(int LocationId)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"
                  SELECT *
                  FROM Sensor
                  WHERE locationid = @locId";
				cmd.Parameters.AddWithValue("@locId", LocationId);
                return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadSensor);
            }
        }

        /// <summary>
        /// Gets all measurements for a sensor with the given identifier.
        /// </summary>
        /// <param name="sensor"></param>
        /// <returns></returns>
		public async Task<IEnumerable<Measurement>> GetMeasurementsAsync(int SensorId)
        {
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = @"
                  SELECT *
                  FROM Measurement
                  WHERE sensorId = @senid";
				cmd.Parameters.AddWithValue("@senid", SensorId);
				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMeasurement);
			}
        }

		/// <summary>
		/// Gets all measurements for a sensor with the given identifier,
		/// within the given timespan.
		/// </summary>
		/// <param name="sensor"></param>
		/// <returns></returns>
		public async Task<IEnumerable<Measurement>> GetMeasurementsAsync(
			string TableName, int SensorId, DateTime Start, DateTime End)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = @"
                    SELECT *
                    " + $"FROM {TableName} as m" + @"
                    WHERE m.sensorId = @id AND m.unixtime >= @starttime AND m.unixtime < @endtime";
				cmd.Parameters.AddWithValue("@id", SensorId);
				cmd.Parameters.AddWithValue("@starttime", DatabaseHelpers.CreateUnixTimeStamp(Start));
				cmd.Parameters.AddWithValue("@endtime", DatabaseHelpers.CreateUnixTimeStamp(End));
				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMeasurement);
			}
		}

		/// <summary>
		/// Gets all measurements for a sensor with the given identifier,
		/// within the given timespan.
		/// </summary>
		/// <param name="sensor"></param>
		/// <returns></returns>
		public Task<IEnumerable<Measurement>> GetMeasurementsAsync(
			int SensorId, DateTime Start, DateTime End)
		{
			return GetMeasurementsAsync(MeasurementTableName, SensorId, Start, End);
		}

        /// <summary>
        /// Inserts the given person data into the Persons table.
        /// </summary>
        /// <param name="Data">The person data to insert into the table.</param>
        public async Task InsertPersonAsync(PersonData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {PersonTableName}(guid, username, name, password, birthdate, address, city, zipcode, isAdmin) " +
                    "VALUES (@guid, @username, @name, @password, @birthdate, @address, @city, @zipcode, @isAdmin)";
                cmd.Parameters.AddWithValue("@guid", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@username", Data.UserName);
                cmd.Parameters.AddWithValue("@name", Data.Name);
                cmd.Parameters.AddWithValue("@password", Data.Password);
                cmd.Parameters.AddWithValue("@birthdate", DatabaseHelpers.CreateUnixTimeStamp(Data.Birthdate));
                cmd.Parameters.AddWithValue("@address", Data.Address);
                cmd.Parameters.AddWithValue("@city", Data.City);
                cmd.Parameters.AddWithValue("@zipcode", Data.ZipCode);
				cmd.Parameters.AddWithValue("@isAdmin", Data.IsAdministrator);
				await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all person data in the given list into the Persons table.
        /// </summary>
        /// <param name="Data">The list of person data to insert into the table.</param>
        public Task InsertPersonAsync(IEnumerable<PersonData> Data)
        {
			return InsertManyAsync(Data, InsertPersonAsync);
        }

        /// <summary>
        /// Inserts the given location data into the Locations table.
        /// </summary>
        /// <param name="Data">The location data to insert into the table.</param>
        public async Task InsertLocationAsync(LocationData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
				cmd.CommandText = $"INSERT INTO {LocationTableName}(name, owner, electricityPrice) " +
					"VALUES (@name, @owner, @electricityPrice)";
                cmd.Parameters.AddWithValue("@name", Data.Name);
                cmd.Parameters.AddWithValue("@owner", Data.OwnerGuidString);
				cmd.Parameters.AddWithValue("@electricityPrice", Data.ElectricityPrice);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all location data in the given list into the Locations table.
        /// </summary>
        /// <param name="Data">The list of location data to insert into the table.</param>
        public Task InsertLocationAsync(IEnumerable<LocationData> Data)
        {
			return InsertManyAsync(Data, InsertLocationAsync);
        }

		/// <summary>
		/// Updates the given location in the Locations table: the data of the location
		/// with the given identifier is updated.
		/// </summary>
		/// <param name="Data">The location to update.</param>
		public async Task UpdateLocationAsync(Location Location)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"UPDATE {LocationTableName} " +
					"SET name = @name, owner = @owner, electricityPrice = @electricityPrice" +
					"WHERE id = @id";
				cmd.Parameters.AddWithValue("@id", Location.Id);
				cmd.Parameters.AddWithValue("@name", Location.Data.Name);
				cmd.Parameters.AddWithValue("@owner", Location.Data.OwnerGuidString);
				cmd.Parameters.AddWithValue("@electricityPrice", Location.Data.ElectricityPrice);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		/// <summary>
		/// Updates the given locations list of locations 
		/// in the Locations table.
		/// </summary>
		public Task UpdateLocationAsync(IEnumerable<Location> Data)
		{
			return InsertManyAsync(Data, UpdateLocationAsync);
		}

        /// <summary>
        /// Updates the sensor in the database
        /// </summary>
        /// <param name="sensor"></param>
        public async Task UpdateSensorAsync(Sensor sensor)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"UPDATE {SensorTableName} " +
                    "SET title = @title, description = @description, notes = @notes " +
                    "WHERE id = @id";

                cmd.Parameters.AddWithValue("@title", sensor.Data.Name);
                cmd.Parameters.AddWithValue("@description", sensor.Data.Description);
                cmd.Parameters.AddWithValue("@notes", sensor.Data.Notes);
                cmd.Parameters.AddWithValue("@id", sensor.Id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts the given sensor data into the Sensor table.
        /// </summary>
        /// <param name="Data">The sensor data to insert into the table.</param>
        public async Task InsertSensorAsync(SensorData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {SensorTableName}(locationid, title, description, notes) " +
                    "VALUES (@locationid, @title, @description, @notes)";
                cmd.Parameters.AddWithValue("@locationid", Data.LocationId);
                cmd.Parameters.AddWithValue("@title", Data.Name);
                cmd.Parameters.AddWithValue("@description", Data.Description);
                cmd.Parameters.AddWithValue("@notes", Data.Notes);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all sensor data in the given list into the Sensor table.
        /// </summary>
        /// <param name="Data">The list of sensor data to insert into the table.</param>
        public Task InsertSensorAsync(IEnumerable<SensorData> Data)
        {
			return InsertManyAsync(Data, InsertSensorAsync);
        }

        /// <summary>
        /// Inserts the given measurement into the table with the given name.
        /// </summary>
        /// <param name="Data">The measurement to insert into the table.</param>
        public async Task InsertMeasurementAsync(Measurement Data, string TableName)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {TableName}(sensorId, unixtime, measured, notes) " +
                    "VALUES (@sensorId, @unixtime, @measured, @notes)";
                cmd.Parameters.AddWithValue("@sensorId", Data.SensorId);
                cmd.Parameters.AddWithValue("@unixtime", DatabaseHelpers.CreateUnixTimeStamp(Data.Time));
                cmd.Parameters.AddWithValue("@measured", Data.MeasuredData);
                cmd.Parameters.AddWithValue("@notes", Data.Notes);
                await cmd.ExecuteNonQueryAsync();
            }
        }

		/// <summary>
		/// Removes the measurement made by the given sensor at the given time 
		/// from the table with the given name.  
		/// </summary>
		/// <remarks>
		/// This method's intended use case is that of clearing the aggregation cache,
		/// not to remove tuples from the Measurements table.
		/// </remarks>
		private async Task DeleteMeasurementAsync(string TableName, int SensorId, DateTime Time)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"DELETE FROM {TableName} " +
					"WHERE sensorId = @sensorId AND unixtime = @unixtime";
				cmd.Parameters.AddWithValue("@sensorId", SensorId);
				cmd.Parameters.AddWithValue("@unixtime", DatabaseHelpers.CreateUnixTimeStamp(Time));
				await cmd.ExecuteNonQueryAsync();
			}
		}

        /// <summary>
        /// Inserts the given measurement into the Measurement table.
        /// </summary>
        /// <param name="Data">The measurement to insert into the table.</param>
        public async Task InsertMeasurementAsync(Measurement Data)
        {
			// Invalidate the aggregation cache first...
			await DeleteMeasurementAsync(
				HourAverageTableName, Data.SensorId, 
				MeasurementAggregation.Quantize(Data.Time, TimeSpan.FromHours(1)));
			await DeleteMeasurementAsync(
				DayAverageTableName, Data.SensorId, 
				MeasurementAggregation.Quantize(Data.Time, TimeSpan.FromDays(1)));
			// ... then insert a tuple into the Measurement table.
            await InsertMeasurementAsync(Data, MeasurementTableName);
        }

        /// <summary>
        /// Inserts all measurements in the given list into the Measurement table.
        /// </summary>
        /// <param name="Data">The list of measurements to insert into the table.</param>
        public Task InsertMeasurementAsync(IEnumerable<Measurement> Data)
        {
			return InsertManyAsync(Data, InsertMeasurementAsync);
        }

        /// <summary>
        /// Inserts the given message  into the Message table.
        /// </summary>
        /// <param name="Data">The message to insert into the table.</param>
        public async Task InsertMessageAsync(MessageData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {MessageTableName}(sender, recipient, message) " +
                    "VALUES (@usender, @recipient, @message)";
                cmd.Parameters.AddWithValue("@usender", Data.SenderGuid.ToString());
                cmd.Parameters.AddWithValue("@recipient", Data.RecipientGuid.ToString());
                cmd.Parameters.AddWithValue("@message", Data.Message);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all messages in the given list into the Message table.
        /// </summary>
        /// <param name="Data">The list of messages to insert into the table.</param>
        public Task InsertMessageAsync(IEnumerable<MessageData> Data)
        {
			return InsertManyAsync(Data, InsertMessageAsync);
        }

        /// <summary>
        /// Inserts the given person-location pair into the
        /// HasLocation table. 
        /// </summary>
        /// <param name="Data">The person-location pair to insert into the table.</param>
        public async Task InsertHasLocationPairAsync(PersonLocationPair Pair)
        {            
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {HasLocationTableName}(personGuid, locationId) " +
                    "VALUES (@personGuid, @locationId)";
                cmd.Parameters.AddWithValue("@personGuid", Pair.PersonGuidString);
                cmd.Parameters.AddWithValue("@locationId", Pair.LocationId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all person-location pairs in the given list into the HasLocation table.
        /// </summary>
        /// <param name="Data">The list of person-location pairs to insert into the table.</param>
        public Task InsertHasLocationPairAsync(IEnumerable<PersonLocationPair> Data)
        {
			return InsertManyAsync(Data, InsertHasLocationPairAsync);
        }

        /// <summary>
        /// Inserts a pair of friends in the Friends table.
        /// </summary>
        public async Task InsertFriendsPairAsync(PersonPair Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO {FriendsTableName}(personOne, personTwo) " +
                    "VALUES (@personOne, @personTwo)";
                cmd.Parameters.AddWithValue("@personOne", Data.PersonOneGuidString);
                cmd.Parameters.AddWithValue("@personTwo", Data.PersonTwoGuidString);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts all person-person pairs in the given list into the Friends table.
        /// </summary>
        /// <param name="Data">The list of person-person pairs to insert into the table.</param>
        public Task InsertFriendsPairAsync(IEnumerable<PersonPair> Data)
        {
			return InsertManyAsync(Data, InsertFriendsPairAsync);
        }

		/// <summary>
		/// Retrieves the set of all sensor tags for the sensor
		/// with the given identifier.
		/// </summary>
		public async Task<IEnumerable<string>> GetSensorTagsAsync(int SensorId)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $@"
                  SELECT t.tag
                  FROM {SensorTagTableName} as t
                  WHERE t.sensorId = @sensorId";
				cmd.Parameters.AddWithValue("@sensorId", SensorId);
				return await ExecuteCommandAsync(cmd, tuple => DatabaseHelpers.GetString(tuple, "tag"));
			}
		}

		/// <summary>
		/// Checks if the database has associated the sensor with 
		/// the given identifier, with the given tag.
		/// </summary>
		public async Task<bool> ContainsSensorTagAsync(int SensorId, string Tag)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $@"
                  SELECT COUNT(*)
                  FROM {SensorTagTableName} as t
                  WHERE t.sensorId = @sensorId AND t.tag = @tag
				  LIMIT 1";
				cmd.Parameters.AddWithValue("@sensorId", SensorId);
				cmd.Parameters.AddWithValue("@tag", Tag.ToLowerInvariant());
                return (Int64) await cmd.ExecuteScalarAsync() > 0;
			}
		}

		/// <summary>
		/// Adds the given tag to the given sensor. A boolean is returned
		/// that reports whether a new tuple was inserted into the database.
		/// This does not happen if the given tag was already associated with
		/// the given sensor.
		/// </summary>
		public async Task<bool> InsertSensorTagAsync(int SensorId, string Tag)
		{
			if (await ContainsSensorTagAsync(SensorId, Tag))
				return false;

			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"INSERT INTO {SensorTagTableName}(sensorId, tag) " +
					"VALUES (@sensorId, @tag)";
				cmd.Parameters.AddWithValue("@sensorId", SensorId);
				cmd.Parameters.AddWithValue("@tag", Tag.ToLowerInvariant());
				await cmd.ExecuteNonQueryAsync();
			}
			return true;
		}

		/// <summary>
		/// Adds the given set of tags to the given sensor.
		/// </summary>
		public Task InsertSensorTagAsync(int SensorId, IEnumerable<string> Tags)
		{
			return InsertManyAsync(Tags, tag => InsertSensorTagAsync(SensorId, tag));
		}

		/// <summary>
		/// Removes the given tag from the sensor with the 
		/// given identifier.
		/// </summary>
		public async Task RemoveSensorTagAsync(int SensorId, string Tag)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"DELETE FROM {SensorTagTableName} " +
					"WHERE sensorId = @sensorId AND tag = @tag";
				cmd.Parameters.AddWithValue("@sensorId", SensorId);
				cmd.Parameters.AddWithValue("@tag", Tag.ToLowerInvariant());
				await cmd.ExecuteNonQueryAsync();
			}
		}

		/// <summary>
		/// Removes all tags from the sensor with the given
		/// identifier.
		/// </summary>
		public async Task ClearSensorTagsAsync(int SensorId)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"DELETE FROM {SensorTagTableName} " +
					"WHERE sensorId = @sensorId";
				cmd.Parameters.AddWithValue("@sensorId", SensorId);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		/// <summary>
		/// Gets all sensors that have been tagged by the given 
		/// string.
		/// </summary>
		public async Task<IEnumerable<Sensor>> GetSensorsByTagAsync(string Tag)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"SELECT s.* " +
					$"FROM {SensorTagTableName} as t, {SensorTableName} as s " +
					"WHERE t.sensorId = s.id AND t.tag = @tag";
				cmd.Parameters.AddWithValue("@tag", Tag.ToLowerInvariant());
				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadSensor); 
			}
		}

		/// <summary>
		/// Gets all sensors at the location specified by the given 
		/// unique identifier, that have been tagged by the given string.
		/// </summary>
		public async Task<IEnumerable<Sensor>> GetSensorsAtLocationByTagAsync(int LocationId, string Tag)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				// This orders sensors by total usage 
				// (which may be useful later on, so I'm leaving it here for now).
				//
				// cmd.CommandText = $"SELECT s.id, s.locationId, s.title, s.description, s.notes " +
				//     $"FROM {SensorTagTableName} as t, {SensorTableName} as s, {MeasurementTableName} as m " +
				//     "WHERE t.sensorId = s.id AND m.sensorId = s.id AND s.locationId = @locId AND t.tag = @tag " +
				//     "GROUP BY s.id " +
				//     "ORDER BY SUM(m.measured) DESC";

				cmd.CommandText = $"SELECT s.* " +
					$"FROM {SensorTagTableName} as t, {SensorTableName} as s " +
					"WHERE t.sensorId = s.id AND s.locationId = @locId AND t.tag = @tag";
				cmd.Parameters.AddWithValue("@locId", LocationId);
				cmd.Parameters.AddWithValue("@tag", Tag.ToLowerInvariant());
				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadSensor); 
			}
		}

        /// <summary>
        /// Creates a task that fetches all messages to display on a given
        /// user's feed, i.e. messages sent to that user by friends or by
        /// themselves.
        /// </summary>
        public async Task<IEnumerable<Message>> GetNewsfeedMessagesAsync(Guid PersonGuid)
        {
            // TODO: does this work?
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT m.* FROM Message m WHERE
                        m.recipient = @person
                        AND (m.sender = @person
                            OR EXISTS (SELECT 1 FROM {TwoWayFriendsTableName} t WHERE
                                m.sender = t.personOne AND
                                m.recipient = t.personTwo))";
                cmd.Parameters.AddWithValue("@person", PersonGuid);
                return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMessage);
            }
        }
        private async Task<IEnumerable<WallPost>> ConvertMessagesToWallPosts(IEnumerable<Message> messages)
        {
            var wallposts = new List<WallPost>();
            foreach (var m in messages)
            {
                wallposts.Add(new WallPost(
                    (await GetPersonByGuidAsync(m.Data.SenderGuid)).Data.UserName,
                    (await GetPersonByGuidAsync(m.Data.RecipientGuid)).Data.UserName,
                    m.Data.Message)
                );
            }
            return wallposts;
        } 
        /// <summary>
        /// Creates a task to fetch all wallposts for a given user. 
        /// Retrieves all messages sent to that user, currently no private messaging is implemented.
        /// TODO: private messaging(?)
        /// </summary>
        public async Task<IEnumerable<WallPost>> GetWallPostsAsync(Guid personGuid)
        {
            var cmd = sqlite.CreateCommand();
            cmd.CommandText = "SELECT * FROM Message WHERE recipient=@person";
            cmd.Parameters.AddWithValue("@person", personGuid.ToString());
            return await ConvertMessagesToWallPosts(await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMessage));
        }
        /// <summary>
        /// Creates a task to fetch all the groups for a given user.
        /// ie, all the groups that that user is a member of
        /// </summary>
        
        public async Task<IEnumerable<Group>> GetGroupsForUserAsync(Guid personGuid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
            
                cmd.CommandText = @"SELECT * 
                FROM PersonGroup 
                WHERE @personGuid 
                IN (
                SELECT person 
                FROM BelongsTo 
                WHERE PersonGroup.id=BelongsTo.personGroup)";
                cmd.Parameters.AddWithValue("@personGuid", personGuid.ToString());
                var groups = await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadGroup);
                foreach (var group in groups)
                {
                    group.MemberList = (await GetMembersForGroupAsync(group.Id)).ToList();
                }

                return groups;
            }
        }
        public async Task<IEnumerable<WallPost>> GetPostsForGroupAsync(long groupid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM Message WHERE recipient IN (
                SELECT person FROM BelongsTo WHERE personGroup=@groupid) 
                OR sender IN (
                SELECT person FROM BelongsTo WHERE personGroup=@groupid)";
                cmd.Parameters.AddWithValue("@groupid", groupid);

                return await ConvertMessagesToWallPosts(await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadMessage));
            }
        }
        /// <summary>
        /// Creates a task to fetch a group entity from its id
        /// </summary>
        public async Task<Group> GetGroupByIdAsync(long groupid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM PersonGroup WHERE id=@groupid";
                cmd.Parameters.AddWithValue("@groupid", groupid);
                return await ExecuteCommandSingleAsync(cmd, DatabaseHelpers.ReadGroup);
            }
        }

        /// <summary>
        /// Creates a task to fetch the members for a group
        /// </summary>
        public async Task<IEnumerable<Person>> GetMembersForGroupAsync(long groupid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM Person WHERE Person.guid IN (
                SELECT person FROM BelongsTo WHERE BelongsTo.personGroup=@groupid)";
                cmd.Parameters.AddWithValue("@groupid", groupid);
                return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
            }
        }
        /// <summary>
        /// Inserts a group with name and description, taken from the group object.
        /// </summary>
        public async Task InsertGroupAsync(Group g)
        {
            
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO PersonGroup (name, description)
                VALUES (@name, @desc)";
                cmd.Parameters.AddWithValue("@name", g.Name);
                cmd.Parameters.AddWithValue("@desc", g.Description);
                await cmd.ExecuteNonQueryAsync();
                cmd.CommandText = "SELECT last_insert_rowid()";
                var i = await cmd.ExecuteScalarAsync();
                cmd.CommandText = "SELECT id from PersonGroup WHERE rowid=" + (long)i;
                i = await cmd.ExecuteScalarAsync();
                Console.WriteLine(i);
                g.Id = (long)i;
                
                await InsertGroupMembersAsync(g);
            }

        }
        /// <summary>
        /// Inserts the members present in the group object (Group::MemberList) into the database
        /// </summary>
        private async Task InsertGroupMembersAsync(Group g)
        {
            foreach (var m in g.MemberList)
            {
                using (var cmd = sqlite.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO BelongsTo (personGroup, person)
                VALUES (@groupId, @personGuid)";
                    cmd.Parameters.AddWithValue("@groupId", g.Id);
                    cmd.Parameters.AddWithValue("@personGuid", m.Guid.ToString());

                    await cmd.ExecuteNonQueryAsync();
                }
                    
            }
        }


        /// <summary>
        /// Close the database connection.
        /// </summary>
        public void Dispose()
		{
			sqlite.Close();
		}

        /// <summary>
        /// Open a database connection, perform a single operation, and close it,
        /// asynchronously retrieving the result. The operation is wrapped
		/// in a transaction.
        /// </summary>
        public static async Task<T> Ask<T>(Func<DataConnection, Task<T>> operation)
        {
            using (var dc = await CreateAsync())
                return await dc.PerformTransaction(operation);
        }

        /// <summary>
        /// Open a database connection, perform a single operation, and close it.
		/// The operation is wrapped in a transaction.
        /// </summary>
        public static async Task Ask(Func<DataConnection, Task> operation)
        {
            using (var dc = await CreateAsync())
				await dc.PerformTransaction(operation);
        }
    }
}
