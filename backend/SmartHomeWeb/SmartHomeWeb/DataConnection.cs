using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Mono.Data.Sqlite;
using System.Threading.Tasks;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
    public class DataConnection : IDisposable
    {
		public const string PersonTableKey = "Person";
		public const string LocationTableKey = "Location";
		public const string HasLocationTableKey = "HasLocation";
		public const string SensorTableKey = "Sensor";

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
		    await Console.Out.WriteLineAsync("Opened database.");
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
			string TableName, Func<IDataRecord, TItem> ReadTuple, Func<TItem, TKey> GetKey)
		{
			var items = await GetTableAsync<TItem>(TableName, ReadTuple);
			return items.ToDictionary(GetKey);
		}

        /// <summary>
        /// Creates a task that fetches all persons in the database.
        /// </summary>
        public Task<IEnumerable<Person>> GetPersonsAsync()
        {
            return GetTableAsync<Person>(PersonTableKey, DatabaseHelpers.ReadPerson);
        }

        /// <summary>
        /// Creates a task that fetches all locations in the database.
        /// </summary>
        public Task<IEnumerable<Location>> GetLocationsAsync()
		{
			return GetTableAsync<Location>(LocationTableKey, DatabaseHelpers.ReadLocation);
		}

		/// <summary>
		/// Creates a task that fetches all person-location pairs
		/// from the has-location table.
		/// </summary>
		public Task<IEnumerable<PersonLocationPair>> GetHasLocationPairsAsync()
		{
			return GetTableAsync<PersonLocationPair>(HasLocationTableKey, DatabaseHelpers.ReadPersonLocationPair);
		}

		/// <summary>
		/// Creates a task that fetches all sensors in the database.
		/// </summary>
		public Task<IEnumerable<Sensor>> GetSensorsAsync()
		{
			return GetTableAsync<Sensor>(SensorTableKey, DatabaseHelpers.ReadSensor);
		}

        /// <summary>
        /// Creates a task that fetches a single row from the database, by
        /// looking up one of its keys.
        /// </summary>
        public Task<TItem> GetSingleByKeyAsync<TKey, TItem>(string TableName, string KeyName, TKey KeyValue, Func<IDataRecord, TItem> ReadTuple)
        {
            using (var cmd = sqlite.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM {TableName} WHERE {KeyName} = @v LIMIT 1";
                cmd.Parameters.AddWithValue("@v", KeyValue);
                return ExecuteCommandSingleAsync(cmd, ReadTuple);
            }
        }

        /// <summary>
        /// Creates a task that fetches a single person from the database.
        /// </summary>
        public Task<Person> GetPersonByIdAsync(int id) =>
            GetSingleByKeyAsync(PersonTableKey, "id", id,
                DatabaseHelpers.ReadPerson);

        /// <summary>
        /// Creates a task that fetches a single location from the database.
        /// </summary>
        public Task<Location> GetLocationByIdAsync(int id) =>
            GetSingleByKeyAsync(LocationTableKey, "id", id,
                DatabaseHelpers.ReadLocation);

        /// <summary>
		/// Creates a dictionary maps persons to their associated locations.
		/// </summary>
		public async Task<IReadOnlyDictionary<Person, IReadOnlyList<Location>>> GetPersonToLocationsMapAsync()
		{
			// First, retrieve all locations, persons and person-location pairs.
			var locTask = GetTableMapAsync<Location, int>(LocationTableKey, DatabaseHelpers.ReadLocation, item => item.Id);
			var personTask = GetTableMapAsync<Person, int>(PersonTableKey, DatabaseHelpers.ReadPerson, item => item.Id);
			var pairs = await GetHasLocationPairsAsync();
			var locs = await locTask;
			var persons = await personTask;

			// Then construct a dictionary.
			var results = new Dictionary<Person, IReadOnlyList<Location>>();
			foreach (var person in persons.Values)
			{
				// Create one location list per person.
				results[person] = new List<Location>();
			}
			foreach (var pair in pairs)
			{
				// Convert every person-location pair to a location
				// list item.
				var list = (List<Location>)results[persons[pair.PersonId]];
				list.Add(locs[pair.LocationId]);
			}

			return results;
		}

		/// <summary>
		/// Creates a task that eagerly fetches all locations that are
		/// associated with the given person in the database.
		/// </summary>
		public Task<IEnumerable<Location>> GetPersonLocationsAsync(Person Item)
		{
		    using (var cmd = sqlite.CreateCommand())
		    {
		        cmd.CommandText = @"
                  SELECT loc.id, loc.name
				  FROM HasLocation as hasLoc, Location as loc 
				  WHERE loc.id = hasLoc.locationId AND hasLoc.personId = @id";
		        cmd.Parameters.AddWithValue("@id", Item.Id);
		        return ExecuteCommandAsync(cmd, DatabaseHelpers.ReadLocation);
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
                cmd.CommandText = "INSERT INTO Person(name) VALUES (@name)";
                cmd.Parameters.AddWithValue("@name", Data.Name);
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


        public void Dispose()
        {
            Console.WriteLine("Closed database.");
            sqlite.Close();
        }

        /// <summary>
		/// Open a database connection, perform a single operation, and close it,
		/// asynchronously retrieving the result.
		/// </summary>
		public static async Task<T> Ask<T>(Func<DataConnection, Task<T>> operation)
        {
            using (var dc = await CreateAsync())
            {
                await Console.Out.WriteLineAsync("Doing the thing.");
                return await operation(dc);
            }
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
