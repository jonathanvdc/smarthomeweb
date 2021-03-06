﻿using System;
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
		public const string FrozenPeriodTableName = "FrozenPeriod";

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
		public static async Task<DataConnection> CreateAsync(
			string JournalMode = "MEMORY", string Synchronous = "NORMAL")
        {
            var result = new DataConnection();
            await result.sqlite.OpenAsync();
			result.SetJournalingMode(JournalMode, Synchronous);
            return result;
        }

		/// <summary>
		/// Sets the journaling mode.
		/// </summary>
		public void SetJournalingMode(string Mode, string Synchronous)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"PRAGMA journal_mode = {Mode}";
				cmd.ExecuteNonQuery();
				cmd.CommandText = $"PRAGMA synchronous = {Synchronous}";
				cmd.ExecuteNonQuery();
                cmd.CommandText = $"PRAGMA foreign_keys = ON";
                cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Creates a new command.
		/// </summary>
		public SqliteCommand CreateCommand()
		{
			return sqlite.CreateCommand();
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
		/// Creates a task that fetches all locations and their owner usernames
		/// in the database.
		/// </summary>
		public async Task<IEnumerable<Tuple<Location, string>>> GetLocationsAndOwnerNamesAsync()
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $@"
                  SELECT loc.*, p.username FROM
				  {LocationTableName} as loc, {PersonTableName} as p
				  WHERE loc.owner = p.guid";

				return await ExecuteCommandAsync(cmd, item =>
					Tuple.Create(DatabaseHelpers.ReadLocation(item), DatabaseHelpers.GetString(item, "username")));
			}
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
            var sensors = await GetSensorsAsync();
            var items = new List<Tuple<Sensor, IEnumerable<string>>>();
			foreach (var sensor in sensors)
			{
				var tags = await GetSensorTagsAsync(sensor.Id);
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
        public Task<IEnumerable<Measurement>> GetRawMeasurementsAsync()
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
                  FROM {PendingFriendRequestTableName} as pair, {PersonTableName} as friend2
                  WHERE pair.personOne = friend2.guid AND pair.personTwo = @guid";
				cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());

				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
			}
		}

        /// <summary>
        /// Creates a task that counts the number of open friend requests to the
        /// user with the given Guid.
        /// </summary>
        public async Task<long> GetRecievedFriendRequestsCountAsync(Guid PersonGuid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $@"
                  SELECT Count( * )
                  FROM {PendingFriendRequestTableName} as pair, {PersonTableName} as friend2
                  WHERE pair.personOne = friend2.guid AND pair.personTwo = @guid";
                cmd.Parameters.AddWithValue("@guid", PersonGuid.ToString());
                return (long)await cmd.ExecuteScalarAsync();
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
		/// Actually computes the month average for the
		/// given sensor during the given month.
		/// </summary>
		private async Task<Measurement> ComputeYearAverageAsync(int SensorId, DateTime Year)
		{
			// Fetch twelve months worth of data from the database, and aggregate those.
			var measurements = await GetMonthAveragesAsync(SensorId, Year, 12);

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
		/// Gets all measurements made in the period defined by
		/// the given start and end date-times. If these measurements
		/// have been compacted, then they will be synthesized from
		/// hour-average data.
		/// </summary>
		public Task<IEnumerable<Measurement>> GetMeasurementsAsync(
			int SensorId, DateTime StartTime, DateTime EndTime)
		{
			if (EndTime < StartTime)
				throw new ArgumentException($"{nameof(StartTime)} was greater than {nameof(EndTime)}");

			var cache = new AggregationCache(this, SensorId, StartTime, EndTime);
			return cache.GetMeasurementsAsync(StartTime, EndTime);
		}

        /// <summary>
        /// Creates a task that fetches or computes the hour average for the
        /// given sensor during the given hour.
        /// </summary>
        public Task<Measurement> GetHourAverageAsync(int SensorId, DateTime Hour)
        {
			return GetHourAveragesAsync(SensorId, Hour, 1).ContinueWith(t => t.Result.Single());
        }

		/// <summary>
		/// Creates a task that fetches or computes a number of hour averages for the
		/// given sensor during the given hours.
		/// </summary>
		public async Task<IEnumerable<Measurement>> GetHourAveragesAsync(int SensorId, DateTime StartHour, int Count)
		{
			var cache = new AggregationCache(this, SensorId, StartHour, StartHour.AddHours(Count));

			var result = await cache.GetHourAveragesAsync(StartHour, Count);
			cache.FlushResults();
			return result;
		}

        /// <summary>
        /// Creates a task that fetches or computes the day average for the
        /// given sensor during the given day.
        /// </summary>
        public Task<Measurement> GetDayAverageAsync(int SensorId, DateTime Day)
        {
			return GetDayAveragesAsync(SensorId, Day, 1).ContinueWith(t => t.Result.Single());
        }

		/// <summary>
		/// Creates a task that fetches or computes a number of day-averages for the
		/// given sensor during the given days.
		/// </summary>
		public async Task<IEnumerable<Measurement>> GetDayAveragesAsync(int SensorId, DateTime StartDay, int Count)
		{
			var cache = new AggregationCache(this, SensorId, StartDay, StartDay.AddDays(Count));

			var result = await cache.GetDayAveragesAsync(StartDay, Count);
			cache.FlushResults();
			return result;
		}

        /// <summary>
		/// Creates a task that fetches or computes a number of day-average totals
		/// of all sensors linked to the given person during the given days.
		/// </summary>
		public async Task<double[]> GetTotalDayAveragesAsync(Guid PersonGuid, DateTime StartDay, int Count)
        {
            var locations = await GetLocationsForPersonAsync(PersonGuid);
            var sensors = (await Task.WhenAll(locations.Select(GetSensorsAtLocationAsync))).SelectMany(s => s);

            double[] total = new double[Count];
            foreach (var sensor in sensors)
            {
                // This always returns an enumerable of `Count` values.
                var measurements = await GetDayAveragesAsync(sensor.Id, StartDay, Count);
                var array = measurements as Measurement[] ?? measurements.ToArray();

                // So this loop is safe.
                var i = 0;
                foreach (var measurement in array)
                {
                    total[i++] += measurement.MeasuredData ?? 0;
                }
            }

            return total;
        }

        /// <summary>
        /// Creates a task that fetches or computes the month average for the
        /// given sensor during the given month.
        /// </summary>
        public Task<Measurement> GetMonthAverageAsync(int SensorId, DateTime Month)
		{
			return GetMonthAveragesAsync(SensorId, Month, 1).ContinueWith(t => t.Result.Single());
		}

		/// <summary>
		/// Creates a task that fetches or computes a number of month averages for the
		/// given sensor during the given month.
		/// </summary>
		public async Task<IEnumerable<Measurement>> GetMonthAveragesAsync(int SensorId, DateTime StartMonth, int Count)
		{
			var cache = new AggregationCache(this, SensorId, StartMonth, StartMonth.AddMonths(Count));

			var result = await cache.GetMonthAveragesAsync(StartMonth, Count);
			cache.FlushResults();
			return result;
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
		/// Creates a task that fetches or computes the year average for the
		/// given sensor during the given year.
		/// </summary>
		public Task<IEnumerable<Measurement>> GetYearAveragesAsync(int SensorId, DateTime StartYear, int Count)
		{
			var results = new Task<Measurement>[Count];
			var time = StartYear;
			for (int i = 0; i < Count; i++)
			{
				results[i] = GetYearAverageAsync(SensorId, time);
				time = time.AddYears(1);
			}
			return Task.WhenAll(results).ContinueWith<IEnumerable<Measurement>>(t => t.Result);
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
        /// Creates a task that returns persons from the database whose username or name fields
        /// contain the given search string.
        /// </summary>
        public async Task<IEnumerable<Person>> GetPersonsBySearchString(string search)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT * FROM {PersonTableName} p
                    WHERE p.name LIKE '%' || @search || '%'
                       OR p.userName LIKE '%' || @search || '%'";
                cmd.Parameters.AddWithValue("search", search);
                return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadPerson);
            }
        }


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
        public Task<Measurement> GetRawMeasurementAsync(int sensorId, long timestamp)
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
		public Task<Measurement> GetRawMeasurementAsync(int sensorId, DateTime timestamp)
        {
			return GetRawMeasurementAsync(sensorId, DatabaseHelpers.CreateUnixTimeStamp(timestamp));
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
        /// Gets all distinct tags at a given location
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        public Task<IEnumerable<string>> GetTagsAtLocationAsync(Location loc)
        {
            return GetTagsAtLocationAsync(loc.Id);
        }

        /// <summary>
        /// Gets all distinct tags at the location with the given identifier.
        /// </summary>
        /// <param name="LocationId">The location's unique identifier.</param>
        /// <returns></returns>
		public async Task<IEnumerable<string>> GetTagsAtLocationAsync(int LocationId)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $@"
                  SELECT DISTINCT t.tag
                  FROM {SensorTableName} AS s, {SensorTagTableName} as t
                  WHERE s.locationid = @locId AND s.id = t.sensorId";
                cmd.Parameters.AddWithValue("@locId", LocationId);
                return await ExecuteCommandAsync(cmd, tuple => DatabaseHelpers.GetString(tuple, "tag"));
            }
        }

        /// <summary>
        /// Gets all measurements for a sensor with the given identifier.
        /// </summary>
        /// <param name="sensor"></param>
        /// <returns></returns>
		public async Task<IEnumerable<Measurement>> GetRawMeasurementsAsync(int SensorId)
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
				cmd.CommandText = $@"
                    SELECT *
                    FROM {TableName} as m
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
		public Task<IEnumerable<Measurement>> GetRawMeasurementsAsync(
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
        /// Delete the person with the given GUID from the Person table.
        /// </summary>
        /// <param name="guid">The person's GUID.</param>
        public async Task DeletePersonAsync(Guid guid)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {PersonTableName} WHERE guid = @guid";
                cmd.Parameters.AddWithValue("@guid", guid.ToString());
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts the given location data into the Location table.
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
        /// Inserts all location data in the given list into the Location table.
        /// </summary>
        /// <param name="Data">The list of location data to insert into the table.</param>
        public Task InsertLocationAsync(IEnumerable<LocationData> Data)
        {
			return InsertManyAsync(Data, InsertLocationAsync);
        }

		/// <summary>
		/// Updates the given location in the Location table: the data of the location
		/// with the given identifier is updated.
		/// </summary>
		/// <param name="Data">The location to update.</param>
		public async Task UpdateLocationAsync(Location Location)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $@"UPDATE {LocationTableName}
					SET name = @name, owner = @owner, electricityPrice = @electricityPrice
					WHERE id = @id";
				cmd.Parameters.AddWithValue("@id", Location.Id);
				cmd.Parameters.AddWithValue("@name", Location.Data.Name);
				cmd.Parameters.AddWithValue("@owner", Location.Data.OwnerGuidString);
				cmd.Parameters.AddWithValue("@electricityPrice", Location.Data.ElectricityPrice);
				await cmd.ExecuteNonQueryAsync();
			}
		}

        /// <summary>
        /// Delete the location with the given ID from the Location table.
        /// </summary>
        /// <param name="LocationId">The location's ID.</param>
        public async Task DeleteLocationAsync(int LocationId)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {LocationTableName} WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", LocationId.ToString());
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
        /// Updates the person in the database
        /// </summary>
        /// <param name="person">The person that's updated</param>
        public async Task UpdatePersonAsync(Person person)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"UPDATE {PersonTableName} " +
                    "SET name = @name, birthdate = @birthdate, address = @address, city = @city, zipcode = @zipcode, password = @password " +
                    "WHERE username = @username";

                cmd.Parameters.AddWithValue("@name", person.Data.Name);
                cmd.Parameters.AddWithValue("@birthdate", DatabaseHelpers.CreateUnixTimeStamp(person.Data.Birthdate));
                cmd.Parameters.AddWithValue("@address", person.Data.Address);
                cmd.Parameters.AddWithValue("@city", person.Data.City);
                cmd.Parameters.AddWithValue("@zipcode", person.Data.ZipCode);
                cmd.Parameters.AddWithValue("@password", person.Data.Password);
                cmd.Parameters.AddWithValue("@username", person.Data.UserName);
                await cmd.ExecuteNonQueryAsync();
            }
        }


        /// <summary>
        /// Updates the notes of the measurement in the database whose
		/// sensor ID and timestamp match the given measurement.
        /// </summary>
        public async Task UpdateMeasurementNotesAsync(Measurement measurement, string tablename)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $@"UPDATE {tablename}
                    SET notes = @notes
					WHERE sensorId =  @sensor  AND unixtime = @time";
                cmd.Parameters.AddWithValue("@notes", measurement.Notes);
                cmd.Parameters.AddWithValue("@sensor", measurement.SensorId);
                cmd.Parameters.AddWithValue("@time", DatabaseHelpers.CreateUnixTimeStamp(measurement.Time));
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
		/// Deletes the measurement made by the given sensor at the given time
		/// from the table with the given name.
		/// </summary>
		/// <remarks>
		/// This method's intended use case is that of clearing the aggregation cache,
		/// not to delete tuples from the Measurements table.
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
		/// Deletes the measurement made by the given sensor during
		/// the given period of time [StartTime, EndTime)
		/// from the table with the given name.
		/// </summary>
		private async Task DeleteMeasurementAsync(string TableName, int SensorId, DateTime StartTime, DateTime EndTime)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"DELETE FROM {TableName} " +
					"WHERE sensorId = @sensorId AND unixtime >= @startTime AND unixtime < @endTime";
				cmd.Parameters.AddWithValue("@sensorId", SensorId);
				cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
				cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
				await cmd.ExecuteNonQueryAsync();
			}
		}

        /// <summary>
        /// Deletes the measurement made by the given sensor during
        /// the given period of time [StartTime, EndTime)
        /// from the table with the given name.
        /// </summary>
        private async Task DeleteMeasurementAsync(string TableName, DateTime StartTime, DateTime EndTime)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {TableName} " +
                    "WHERE unixtime >= @startTime AND unixtime < @endTime";
                cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
                cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts the given measurement into the Measurement table.
        /// </summary>
        /// <param name="Data">The measurement to insert into the table.</param>
        public async Task InsertMeasurementAsync(Measurement Data)
        {
			if (await IsFrozenAsync(Data.Time))
				throw new ArgumentOutOfRangeException($"{nameof(Data)}'s timestamp was frozen.");

			// Invalidate the aggregation cache first...
			await DeleteMeasurementAsync(
				HourAverageTableName, Data.SensorId,
				MeasurementAggregation.Quantize(Data.Time, TimeSpan.FromHours(1)));
			await DeleteMeasurementAsync(
				DayAverageTableName, Data.SensorId,
				MeasurementAggregation.Quantize(Data.Time, TimeSpan.FromDays(1)));
			await DeleteMeasurementAsync(
				MonthAverageTableName, Data.SensorId,
				MeasurementAggregation.QuantizeMonth(Data.Time));
			await DeleteMeasurementAsync(
				YearAverageTableName, Data.SensorId,
				MeasurementAggregation.QuantizeYear(Data.Time));
			// ... then insert a tuple into the Measurement table.
            await InsertMeasurementAsync(Data, MeasurementTableName);
        }

		private bool HasMeasurementDuring(string TableName, DateTime StartTime, DateTime EndTime, HashSet<int> SensorIds)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"SELECT COUNT(*) FROM {TableName} " +
					"WHERE @startTime <= unixtime AND @endTime >= unixTime AND @sensorId = sensorId " +
					"LIMIT 1";
				cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
				cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
				cmd.Parameters.AddWithValue("@sensorId", 0);

				foreach (var id in SensorIds)
				{
					cmd.Parameters["@sensorId"].Value = id;
					if ((long)cmd.ExecuteScalar() > 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool OverlapsWithFrozenPeriod(DateTime StartTime, DateTime EndTime)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"SELECT COUNT(*) FROM {FrozenPeriodTableName} as frozen " +
					"WHERE frozen.startTime <= @endTime AND @startTime <= frozen.endTime " +
					"LIMIT 1";
				cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
				cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
				return (long)cmd.ExecuteScalar() > 0;
			}
		}

        /// <summary>
        /// Inserts all measurements in the given list into the Measurement table.
        /// </summary>
        /// <param name="Data">The list of measurements to insert into the table.</param>
        public async Task InsertMeasurementAsync(IEnumerable<Measurement> Data)
        {
			// This has to be really fast.
			// That means that there will be no more half-measures.
			//
			// What follows is a horribly monolithic
			// piece of code that has been contructed to extract
			// every bit of performance from the database.
			//
			// Additionally, it uses a number of tricks to
			// make the common case very fast, especially
			// for large data sets: checks are skipped and
			// caches are not invalidated, if the algorithm
			// determines that doing so is not necessary.

			// Compute a time period that encompasses all measurements,
			// and find the measurements' sensor identifiers.
			// We'll use this later on to avoid doing unnecessary work.
			// This will be parallelized heavily, because we're probably
			// working with a pretty big data set.
			var period = await TimeHelpers.ComputeConflictPeriodAsync(Data);

			// Create six commands, which we will re-use.
			SqliteCommand insertCommand = sqlite.CreateCommand(), deleteHourCommand = null, deleteDayCommand = null,
				deleteMonthCommand = null, deleteYearCommand = null, checkFrozenCommand = null;
			try
			{
				// Initialize the commands
				insertCommand.CommandText = $"INSERT INTO {MeasurementTableName}(sensorId, unixtime, measured, notes) " +
					"VALUES (@sensorId, @unixtime, @measured, @notes)";
				insertCommand.Parameters.AddWithValue("@sensorId", 0);
				insertCommand.Parameters.AddWithValue("@unixtime", 0L);
				insertCommand.Parameters.AddWithValue("@measured", 0.0);
				insertCommand.Parameters.AddWithValue("@notes", "");

				// We will initialize and execute these commands if we
				// can't know for sure that we won't have to.
				// We have a good idea of which caches we may have to
				// invalidate, based on the conflict periods we have
				// already computed.
				if (period == null || HasMeasurementDuring(HourAverageTableName, period.Item1, period.Item2, period.Item3))
				{
					deleteHourCommand = sqlite.CreateCommand();
					deleteHourCommand.CommandText = $"DELETE FROM {HourAverageTableName} " +
						"WHERE sensorId = @sensorId AND unixtime = @unixtime";
					deleteHourCommand.Parameters.AddWithValue("@sensorId", 0);
					deleteHourCommand.Parameters.AddWithValue("@unixtime", 0L);
				}

				if (period == null || HasMeasurementDuring(DayAverageTableName, period.Item1, period.Item2, period.Item3))
				{
					deleteDayCommand = sqlite.CreateCommand();
					deleteDayCommand.CommandText = $"DELETE FROM {DayAverageTableName} " +
						"WHERE sensorId = @sensorId AND unixtime = @unixtime";
					deleteDayCommand.Parameters.AddWithValue("@sensorId", 0);
					deleteDayCommand.Parameters.AddWithValue("@unixtime", 0L);
				}

				if (period == null || HasMeasurementDuring(MonthAverageTableName, period.Item1, period.Item2, period.Item3))
				{
					deleteMonthCommand = sqlite.CreateCommand();
					deleteMonthCommand.CommandText = $"DELETE FROM {MonthAverageTableName} " +
						"WHERE sensorId = @sensorId AND unixtime = @unixtime";
					deleteMonthCommand.Parameters.AddWithValue("@sensorId", 0);
					deleteMonthCommand.Parameters.AddWithValue("@unixtime", 0L);
				}

				if (period == null || HasMeasurementDuring(YearAverageTableName, period.Item1, period.Item2, period.Item3))
				{
					deleteYearCommand = sqlite.CreateCommand();
					deleteYearCommand.CommandText = $"DELETE FROM {YearAverageTableName} " +
						"WHERE sensorId = @sensorId AND unixtime = @unixtime";
					deleteYearCommand.Parameters.AddWithValue("@sensorId", 0);
					deleteYearCommand.Parameters.AddWithValue("@unixtime", 0L);
				}

				if (period == null || OverlapsWithFrozenPeriod(period.Item1, period.Item2))
				{
					checkFrozenCommand = sqlite.CreateCommand();
					checkFrozenCommand.CommandText = $"SELECT COUNT(*) FROM {FrozenPeriodTableName} as frozen " +
						"WHERE frozen.startTime <= @time AND frozen.endTime >= @time " +
						"LIMIT 1";
					checkFrozenCommand.Parameters.AddWithValue("@time", 0L);
				}

				foreach (var m in Data)
				{
					// We will now update these commands' parameters,
					// and execute them _synchronously_. SQLite
					// locks the database during a write operation anyway,
					// so there's really no point in task-based parallelism.

					// Invalidate the aggregation cache first...
					if (deleteHourCommand != null)
					{
						deleteHourCommand.Parameters["@sensorId"].Value = m.SensorId;
						deleteHourCommand.Parameters["@unixtime"].Value = DatabaseHelpers.CreateUnixTimeStamp(MeasurementAggregation.Quantize(m.Time, TimeSpan.FromHours(1)));
						deleteHourCommand.ExecuteNonQuery();
					}
					if (deleteDayCommand != null)
					{
						deleteDayCommand.Parameters["@sensorId"].Value = m.SensorId;
						deleteDayCommand.Parameters["@unixtime"].Value = DatabaseHelpers.CreateUnixTimeStamp(MeasurementAggregation.Quantize(m.Time, TimeSpan.FromDays(1)));
						deleteDayCommand.ExecuteNonQuery();
					}
					if (deleteMonthCommand != null)
					{
						deleteMonthCommand.Parameters["@sensorId"].Value = m.SensorId;
						deleteMonthCommand.Parameters["@unixtime"].Value = DatabaseHelpers.CreateUnixTimeStamp(MeasurementAggregation.QuantizeMonth(m.Time));
						deleteMonthCommand.ExecuteNonQuery();
					}
					if (deleteYearCommand != null)
					{
						deleteYearCommand.Parameters["@sensorId"].Value = m.SensorId;
						deleteYearCommand.Parameters["@unixtime"].Value = DatabaseHelpers.CreateUnixTimeStamp(MeasurementAggregation.QuantizeYear(m.Time));
						deleteYearCommand.ExecuteNonQuery();
					}
					if (checkFrozenCommand != null)
					{
						checkFrozenCommand.Parameters["@time"].Value = DatabaseHelpers.CreateUnixTimeStamp(m.Time);
						if ((long)checkFrozenCommand.ExecuteScalar() > 0)
						{
							throw new InvalidOperationException(
								$"Timestamp of {m} overlaps with a known frozen location.");
						}
					}

					// ... then insert a tuple into the Measurement table.
					insertCommand.Parameters["@sensorId"].Value = m.SensorId;
					insertCommand.Parameters["@unixtime"].Value = DatabaseHelpers.CreateUnixTimeStamp(m.Time);
					insertCommand.Parameters["@measured"].Value = m.MeasuredData;
					insertCommand.Parameters["@notes"].Value = m.Notes;
					insertCommand.ExecuteNonQuery();
				}
			}
			finally
			{
				insertCommand.Dispose();
				if (deleteHourCommand != null)
				{
					deleteHourCommand.Dispose();
				}
				if (deleteDayCommand != null)
				{
					deleteDayCommand.Dispose();
				}
				if (deleteMonthCommand != null)
				{
					deleteMonthCommand.Dispose();
				}
				if (deleteYearCommand != null)
				{
					deleteYearCommand.Dispose();
				}
				if (checkFrozenCommand != null)
				{
					checkFrozenCommand.Dispose();
				}
			}
        }

		/// <summary>
		/// Determines if the specified timestamp has been frozen: no more
		/// measurements can be inserted at that time.
		/// </summary>
		/// <returns><c>true</c> if the given timestamp is frozen; otherwise, <c>false</c>.</returns>
		public async Task<bool> IsFrozenAsync(DateTime Time)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"SELECT COUNT(*) FROM {FrozenPeriodTableName} as frozen " +
					"WHERE frozen.startTime <= @time AND frozen.endTime >= @time " +
					"LIMIT 1";
				cmd.Parameters.AddWithValue("@time", DatabaseHelpers.CreateUnixTimeStamp(Time));
				return (long)await cmd.ExecuteScalarAsync() > 0;
			}
		}

		/// <summary>
		/// Reads all frozen periods from the database.
		/// </summary>
		public Task<IEnumerable<FrozenPeriod>> GetFrozenPeriodsAsync()
		{
			return GetTableAsync(FrozenPeriodTableName, DatabaseHelpers.ReadFrozenPeriod);
		}

		/// <summary>
		/// Reads all frozen periods from the database that completely or
		/// partially overlap with the given period of time.
		/// </summary>
		public async Task<IEnumerable<FrozenPeriod>> GetFrozenPeriodsAsync(DateTime StartTime, DateTime EndTime)
		{
			if (EndTime < StartTime)
				throw new ArgumentException($"{nameof(StartTime)} was greater than {nameof(EndTime)}");

			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"SELECT * FROM {FrozenPeriodTableName} as frozen " +
					"WHERE frozen.startTime <= @endTime AND @startTime <= frozen.endTime";
				cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
				cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
				return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadFrozenPeriod);
			}
		}

		/// <summary>
		/// Freezes the period of time specified by the given start-time
		/// and end-time.
		/// </summary>
		public async Task FreezeAsync(
			DateTime StartTime, DateTime EndTime, CompactionLevel Compaction = CompactionLevel.None)
		{
			if (EndTime < StartTime)
				throw new ArgumentException($"{nameof(StartTime)} was greater than {nameof(EndTime)}");

			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"DELETE FROM {FrozenPeriodTableName} " +
					"WHERE startTime >= @startTime AND endTime <= @endTime " +
					"  AND compactionLevel <= @compactionLevel";
				cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
				cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
				cmd.Parameters.AddWithValue("@compactionLevel", (int)Compaction);
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd = sqlite.CreateCommand())
			{
				// Don't insert a tuple into the database if there
				// is a pre-existing tuple that encompasses this
				// one.
				cmd.CommandText = $"SELECT COUNT (*) FROM {FrozenPeriodTableName} " +
					"WHERE startTime <= @startTime AND endTime >= @endTime " +
					"  AND compactionLevel >= @compactionLevel " +
					"LIMIT 1";
				cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
				cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
				cmd.Parameters.AddWithValue("@compactionLevel", (int)Compaction);
				if ((long)await cmd.ExecuteNonQueryAsync() > 0)
					return;
			}

			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = $"INSERT INTO {FrozenPeriodTableName}(startTime, endTime, compactionLevel) " +
					"VALUES (@startTime, @endTime, @compactionLevel)";
				cmd.Parameters.AddWithValue("@startTime", DatabaseHelpers.CreateUnixTimeStamp(StartTime));
				cmd.Parameters.AddWithValue("@endTime", DatabaseHelpers.CreateUnixTimeStamp(EndTime));
				cmd.Parameters.AddWithValue("@compactionLevel", (int)Compaction);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		/// <summary>
		/// Freezes the given period of time.
		/// </summary>
		public Task FreezeAsync(FrozenPeriod Period)
		{
			return FreezeAsync(Period.StartTime, Period.EndTime, Period.Compaction);
		}

		/// <summary>
		/// Reclaims free storage from the database.
		/// Cannot be performed from within a transaction,
		/// i.e. within the scope of an Ask call.
		/// </summary>
		public async Task VacuumAsync()
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = "VACUUM";
				await cmd.ExecuteNonQueryAsync();
			}
		}

		/// <summary>
		/// Compacts the given period of time. First, all data in the given
		/// period of time is aggregated, then all measurements are deleted.
		/// The given period of time is subsequently frozen: further
		/// measurement insertion is disallowed during this period.
		/// </summary>
		public async Task CompactAsync(
            DateTime StartTime, DateTime EndTime,
            CompactionLevel Level = CompactionLevel.Measurements)
		{
			if (EndTime < StartTime)
				throw new ArgumentException($"{nameof(EndTime)} was greater than {nameof(StartTime)}");

            // Figure out which quantization scheme we should use.
            // Also create an aggregation function and a list
            // of tables that are to be compacted.
            Func<DateTime, DateTime> compactQuantizer;
            Func<DateTime, DateTime> freezeQuantizer;
            Func<int, DateTime, DateTime, Task> aggregate;
            string[] compactedTableNames;
            switch (Level)
            {
                case CompactionLevel.DayAverages:
                    compactQuantizer = MeasurementAggregation.QuantizeDay;
                    freezeQuantizer = MeasurementAggregation.QuantizeMonth;
                    aggregate = async (id, start, end) =>
                    {
                        // Compute month averages.
                        var cache = new AggregationCache(this, id, start, end);
                        for (var t = start; t < end; t = t.AddMonths(1))
                        {
                            await cache.GetMonthAverageAsync(t);
                        }
                        cache.DiscardHourAverages();
                        cache.DiscardDayAverages();
                        cache.FlushResults();
                    };
                    compactedTableNames = new string[] { DayAverageTableName, HourAverageTableName, MeasurementTableName };
                    break;
                case CompactionLevel.HourAverages:
                    compactQuantizer = MeasurementAggregation.QuantizeHour;
                    freezeQuantizer = MeasurementAggregation.QuantizeDay;
                    aggregate = async (id, start, end) =>
                    {
                        // Compute day averages.
                        int dayCount = (int)Math.Ceiling((end - start).TotalDays);

                        var cache = new AggregationCache(this, id, start, end);
                        await cache.GetDayAveragesAsync(start, dayCount);
                        cache.DiscardHourAverages();
                        cache.FlushResults();
                    };
                    compactedTableNames = new string[] { HourAverageTableName, MeasurementTableName };
                    break;
                case CompactionLevel.Measurements:
                    compactQuantizer = dt => dt;
                    freezeQuantizer = MeasurementAggregation.QuantizeHour;
                    aggregate = (id, start, end) =>
                    {
                        // Compute hour averages.
                        int hourCount = (int)Math.Ceiling((end - start).TotalHours);
                        return GetHourAveragesAsync(id, start, hourCount);
                    };
                    compactedTableNames = new string[] { MeasurementTableName };
                    break;
                case CompactionLevel.None:
                default:
                    compactQuantizer = dt => dt;
                    freezeQuantizer = dt => dt;
                    aggregate = (id, start, end) => Task.FromResult(true);
                    compactedTableNames = new string[] { };
                    break;
            }

            // Subdivide the period of time we'd like to subdivide
            // compact.
            var subdiv = await AggregationCache.PartitionByCompaction(
                this, compactQuantizer(StartTime), compactQuantizer(EndTime), Level);

            // Get all sensors in the database.
            var allSensors = (await GetSensorsAsync()).ToArray();

            // Aggregate data for all of these sensors.
            // Try to parallelize that as much as possible, too.
            var aggregationTasks = new Task[allSensors.Length];
            for (int i = 0; i < allSensors.Length; i++)
            {
                aggregationTasks[i] = aggregate(
                    allSensors[i].Id, freezeQuantizer(StartTime),
                    freezeQuantizer(EndTime));
            }
            await Task.WhenAll(aggregationTasks);

            foreach (var item in subdiv)
            {
                // Compact all ranges which have the same compaction
                // level as this function's given compaction level.
                if (item.Compaction == Level)
                {
                    foreach (var table in compactedTableNames)
                    {
                        // Discard measurements.
                        await DeleteMeasurementAsync(table, item.StartTime, item.EndTime);
                    }

                    // Freeze this period of time.
                    await FreezeAsync(item.StartTime, item.EndTime, Level);
                }
            }

            // Finally, freeze measurements in the vicinity, to keep
            // aggregation from going haywire.
            await FreezeAsync(
                freezeQuantizer(StartTime), freezeQuantizer(EndTime), Level);
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
                return (long)await cmd.ExecuteScalarAsync() > 0;
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
		/// Deletes the given tag from the sensor with the
		/// given identifier.
		/// </summary>
		public async Task DeleteSensorTagAsync(int SensorId, string Tag)
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
		/// Deletes all tags from the sensor with the given
		/// identifier.
		/// </summary>
		public async Task DeleteAllSensorTagsAsync(int SensorId)
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
        /// <summary>
        /// Creates a task to fetch all wallposts for a given user.
        /// Retrieves all messages sent to that user, currently no private messaging is implemented.
        /// TODO: private messaging(?)
        /// </summary>
        public async Task<IEnumerable<WallPost>> GetWallPostsAsync(Guid personGuid)
        {
            IEnumerable<WallPost> results;
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM Message as m
                LEFT JOIN HasAttachment as ha ON (m.id == ha.message_Id)
                LEFT JOIN Graph as g ON (ha.graph_Id == g.graphId)
                INNER JOIN Person as p1 ON (m.sender = p1.guid)
                INNER JOIN Person as p2 ON (m.recipient = p2.guid)
                WHERE m.recipient=@person";

                cmd.Parameters.AddWithValue("@person", personGuid.ToString());
                results = await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadWallPost);
            }
            return await Task.WhenAll(results.Select(WithGraphElementsAsync));
        }

        public async Task InsertWallPostAsync(WallPost wp)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Message (sender, recipient, message) VALUES (@sender, @recipient, @message)";
                cmd.Parameters.AddWithValue("@sender", wp.Source.GuidString);
                cmd.Parameters.AddWithValue("@recipient", wp.Destination.GuidString);
                cmd.Parameters.AddWithValue("@message", wp.Message);
                await cmd.ExecuteNonQueryAsync();
                cmd.CommandText = "SELECT last_insert_rowid()";
                var i = await cmd.ExecuteScalarAsync();
                cmd.CommandText = "SELECT id from Message WHERE rowid=" + (long)i;
                i = await cmd.ExecuteScalarAsync();
                if (wp.Image != null)
                {
                    cmd.CommandText = @"INSERT INTO HasAttachment (message_Id, graph_Id) VALUES (@id, @id2)";
                    cmd.Parameters.AddWithValue("@id", i);
                    cmd.Parameters.AddWithValue("@id2", wp.Image.Id);

                    await cmd.ExecuteNonQueryAsync();
                }

            }
        }

        /// <summary>
        /// Reads all graph elements that belong to the graph with
        /// the given identifier.
        /// </summary>
        private async Task<IEnumerable<AutofitRange>> GetGraphElementsAsync(int GraphId)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM GraphElement WHERE graphId = @id";
                cmd.Parameters.AddWithValue("@id", GraphId);

                return await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadAutofitRange);
            }
        }

        /// <summary>
        /// Updates the given (empty) graph with its graph
        /// elements: the ranges of measurements that it
        /// portrays.
        /// </summary>
        private async Task<Graph> WithGraphElementsAsync(Graph EmptyGraph)
        {
            return new Graph(
                EmptyGraph.Id,
                new GraphData(
                    await GetGraphElementsAsync(EmptyGraph.Id),
                    EmptyGraph.Data.Name, EmptyGraph.Data.OwnerGuid));
        }

        /// <summary>
        /// Updates the given wall post with graph
        /// elements: the ranges of measurements that its
        /// graph portrays.
        /// </summary>
        private async Task<WallPost> WithGraphElementsAsync(WallPost Post)
        {
            return Post.Image == null
                ? Post
                : new WallPost(Post.Id, Post.Source, Post.Destination, Post.Message, await WithGraphElementsAsync(Post.Image));
        }

        public async Task<Graph> GetGraphByIdAsync(int id)
        {
            Graph result;
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Graph WHERE graphId = @id";
                cmd.Parameters.AddWithValue("@id", id);

                result = await ExecuteCommandSingleAsync(cmd, DatabaseHelpers.ReadEmptyGraph);
            }
            return await WithGraphElementsAsync(result);
        }

        /// <summary>
        /// Returns a graph matching the owner and name.
        /// If no such graph is found, null is returned.
        /// </summary>
        public async Task<Graph> GetGraphByOwnerAndNameAsync(Guid OwnerGuid, string GraphName)
        {
            Graph result;
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Graph WHERE owner = @owner AND name = @name";
                cmd.Parameters.AddWithValue("@owner", OwnerGuid.ToString());
                cmd.Parameters.AddWithValue("@name", GraphName);

                result = await ExecuteCommandSingleAsync(cmd, DatabaseHelpers.ReadEmptyGraph);
            }
            return await WithGraphElementsAsync(result);
        }

        /// <summary>
        /// Returns all graphs that belong to the person with the given GUID.
        /// </summary>
        public async Task<IEnumerable<Graph>> GetGraphsByOwnerAsync(Guid OwnerGuid)
        {
            IEnumerable<Graph> results;
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Graph WHERE owner = @owner";
                cmd.Parameters.AddWithValue("@owner", OwnerGuid.ToString());

                results = await ExecuteCommandAsync(cmd, DatabaseHelpers.ReadEmptyGraph);
            }
            return await Task.WhenAll(results.Select(WithGraphElementsAsync));
        }

        public async Task InsertGraphAsync(GraphData Data)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Graph (owner, name) VALUES (@owner, @name)";
                cmd.Parameters.AddWithValue("@owner", Data.OwnerGuidString);
                cmd.Parameters.AddWithValue("@name", Data.Name);

                await cmd.ExecuteNonQueryAsync();
            }

            int graphId = (await GetGraphByOwnerAndNameAsync(Data.OwnerGuid, Data.Name)).Id;

            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO GraphElement (graphId, sensorId, startTime, endTime, maxMeasurements)
                    VALUES (@graphId, @sensorId, @startTime, @endTime, @maxMeasurements)";
                cmd.Parameters.AddWithValue("@graphId", graphId);
                cmd.Parameters.AddWithValue("@sensorId", 0);
                cmd.Parameters.AddWithValue("@startTime", 0L);
                cmd.Parameters.AddWithValue("@endTime", 0L);
                cmd.Parameters.AddWithValue("@maxMeasurements", 0);

                foreach (var elem in Data.Chart)
                {
                    cmd.Parameters["@sensorId"].Value = elem.SensorId;
                    cmd.Parameters["@startTime"].Value = DatabaseHelpers.CreateUnixTimeStamp(elem.StartTime);
                    cmd.Parameters["@endTime"].Value = DatabaseHelpers.CreateUnixTimeStamp(elem.EndTime);
                    cmd.Parameters["@maxMeasurements"].Value = elem.MaxMeasurements;
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
		public static async Task<T> Ask<T>(
			Func<DataConnection, Task<T>> operation,
			string JournalMode = "MEMORY", string Synchronous = "NORMAL")
        {
            using (var dc = await CreateAsync(JournalMode, Synchronous))
                return await dc.PerformTransaction(operation);
        }

        /// <summary>
        /// Open a database connection, perform a single operation, and close it.
		/// The operation is wrapped in a transaction.
        /// </summary>
        public static async Task Ask(
			Func<DataConnection, Task> operation,
			string JournalMode = "MEMORY", string Synchronous = "NORMAL")
        {
            using (var dc = await CreateAsync(JournalMode, Synchronous))
				await dc.PerformTransaction(operation);
        }
    }
}
