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
		/// Creates a task that executes a query, and interprets the results.
		/// </summary>
		/// <param name="Sql">The query to execute.</param>
		/// <param name="ReadTuple">A function that reads a single tuple.</param>
		public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(
			string Sql, 
			Func<IDataRecord, T> ReadTuple)
		{
			var results = new List<T>();
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = Sql;
				using (var reader = await cmd.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						results.Add(ReadTuple(reader));
					}
				}
			}
			return results;
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
			return ExecuteQueryAsync<T>("SELECT * FROM " + TableName, ReadTuple);
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
		    Console.WriteLine("GetPersonsAsync");
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
			return ExecuteQueryAsync<Location>(
				@"SELECT loc.id, loc.name
				  FROM HasLocation as hasLoc, Location as loc 
				  WHERE loc.id = hasLoc.locationId AND hasLoc.personId = " + Item.Id,
				DatabaseHelpers.ReadLocation);
		}

		/// <summary>
		/// Inserts the given item into the given table specification.
		/// </summary>
		/// <param name="TableSpec">A specification of a table, which may include its field names</param>
		/// <param name="Item">The item to insert into the table.</param>
		/// <param name="EncodeItem">A function that encodes the item as a SQL tuple.</param>
		public Task InsertTableAsync<T>(
			string TableSpec, T Item, Func<T, string> EncodeItem)
		{
			using (var cmd = sqlite.CreateCommand())
			{
				cmd.CommandText = "INSERT INTO " + TableSpec + " VALUES (" + EncodeItem(Item) + ")";
				return cmd.ExecuteNonQueryAsync();
			}
		}

		/// <summary>
		/// Asynchronously inserts the given sequence of items 
		/// into the specified table.
		/// </summary>
		public Task InsertTableAsync<T>(
			string TableSpec, IEnumerable<T> Items, Func<T, string> EncodeItem)
		{
			var tasks = new List<Task>();
			foreach (var item in Items)
			{
				tasks.Add(InsertTableAsync<T>(TableSpec, item, EncodeItem));
			}
			return Task.WhenAll(tasks);
		}

		/// <summary>
		/// Asynchronously creates new persons in the database.
		/// </summary>
		public Task InsertPersonsAsync(
			IEnumerable<PersonData> Items)
		{
			return InsertTableAsync<PersonData>("Person(name)", Items, DatabaseHelpers.EncodePersonData);
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
