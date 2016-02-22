using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Mono.Data.Sqlite;
using System.Threading.Tasks;
using AsyncPoco;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
    public class DataConnection : IDisposable
    {
		public const string PersonTableKey = "Person";
		public const string LocationTableKey = "Location";
		public const string HasLocationTableKey = "HasLocation";
		public const string SensorTableKey = "Sensor";

        const string DatabasePath = "../../../../database/smarthomeweb.db";
        const string ConnectionString = "Data Source=" + DatabasePath;


        private SqliteConnection sqlite;
        private Database db;

        public DataConnection()
        {
			sqlite = new SqliteConnection(ConnectionString);
            sqlite.Open();
            db = new Database(sqlite);
        }

		/// <summary>
		/// Creates a task that eagerly fetches all elements from 
		/// the table with the given name.
		/// </summary>
		/// <param name="TableName">The table to fetch items from.</param>
		public async Task<IEnumerable<T>> GetTableAsync<T>(string TableName)
		{
			return await db.FetchAsync<T>("SELECT * FROM " + TableName);
		}

		/// <summary>
		/// Creates a task that eagerly fetches all elements from 
		/// the table with the given name. The given key function is
		/// then used to construct a dictionary that maps these keys
		/// to the items they belong to.
		/// </summary>
		/// <param name="TableName">The table to fetch items from.</param>
		/// <param name="GetKey">A function that extracts primary keys from tuples.</param>
		public async Task<IReadOnlyDictionary<TKey, TItem>> GetTableMapAsync<TItem, TKey>(string TableName, Func<TItem, TKey> GetKey)
		{
			var items = await GetTableAsync<TItem>(TableName);
			return items.ToDictionary(GetKey);
		}

		/// <summary>
		/// Creates a task that fetches all persons in the database.
		/// </summary>
		public Task<IEnumerable<Person>> GetPersonsAsync()
        {
			return GetTableAsync<Person>(PersonTableKey);
        }

		/// <summary>
		/// Creates a task that fetches all locations in the database.
		/// </summary>
		public Task<IEnumerable<Location>> GetLocationsAsync()
		{
			return GetTableAsync<Location>(LocationTableKey);
		}

		/// <summary>
		/// Creates a task that fetches all person-location pairs
		/// from the has-location table.
		/// </summary>
		public Task<IEnumerable<PersonLocationPair>> GetHasLocationPairsAsync()
		{
			return GetTableAsync<PersonLocationPair>(HasLocationTableKey);
		}

		/// <summary>
		/// Creates a dictionary maps persons to their associated locations.
		/// </summary>
		public async Task<IReadOnlyDictionary<Person, IReadOnlyList<Location>>> GetPersonToLocationsMapAsync()
		{
			// First, retrieve all locations, persons and person-location pairs.
			var locTask = GetTableMapAsync<Location, int>(LocationTableKey, item => item.Id);
			var personTask = GetTableMapAsync<Person, int>(PersonTableKey, item => item.Id);
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
		public async Task<IEnumerable<Location>> GetPersonLocationsAsync(Person Item)
		{
			return await db.FetchAsync<Location>(
				@"SELECT loc.id, loc.name
				  FROM HasLocation as hasLoc, Location as loc 
				  WHERE loc.id = hasLoc.locationId AND hasLoc.personId = " + Item.Id);
		}

		/// <summary>
		/// Creates a task that fetches all sensors in the database.
		/// </summary>
		public Task<IEnumerable<Sensor>> GetSensorsAsync()
		{
			return GetTableAsync<Sensor>(SensorTableKey);
		}

		/// <summary>
		/// Asynchronously inserts the given sequence of items 
		/// into the specified table.
		/// </summary>
		public Task<IEnumerable<T>> InsertTableAsync<T>(
			IEnumerable<T> Items,
			string TableName, string PrimaryKeyName)
		{
			var tasks = new List<Task<object>>();
			foreach (var item in Items)
			{
				tasks.Add(db.InsertAsync(TableName, PrimaryKeyName, true, item));
			}
			return Task.WhenAll(tasks).ContinueWith(task => task.Result.Cast<T>());
		}

		/// <summary>
		/// Asynchronously inserts the given sequence of items 
		/// into the specified table.
		/// </summary>
		public Task<IEnumerable<TResult>> InsertTableAsync<TResult, TData>(
			IEnumerable<TData> Items, Func<TData, TResult> ToResult,
			string TableName, string PrimaryKeyName)
		{
			return InsertTableAsync<TResult>(
				Items.Select(ToResult), TableName, PrimaryKeyName);
		}

		/// <summary>
		/// Asynchronously creates new persons in the database.
		/// </summary>
		public Task<IEnumerable<Person>> InsertPersonsAsync(
			IEnumerable<PersonData> Items)
		{
			return InsertTableAsync<Person, PersonData>(
				Items, item => new Person(0, item), 
				PersonTableKey, "id");
		}

        public void Dispose()
        {
            sqlite.Close();
        }
    }
}
