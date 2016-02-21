using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using System.Threading.Tasks;
using AsyncPoco;
using SmartHomeWeb.Model;

namespace SmartHomeWeb
{
    public class DataConnection : IDisposable
    {
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

        public async Task Testy()
        {
            var count = await db.ExecuteScalarAsync<long>("SELECT count(*) FROM Person");
            Console.WriteLine(count);
        }

		/// <summary>
		/// Creates a task that eagerly fetches all elements from 
		/// the table with the given name.
		/// </summary>
		/// <param name="TableName">The table to fetch items from.</param>
		public async Task<IEnumerable<T>> GetAllAsync<T>(string TableName)
		{
			return await db.FetchAsync<T>("SELECT * FROM " + TableName);
		}

		/// <summary>
		/// Creates a task that fetches all persons in the database.
		/// </summary>
		public Task<IEnumerable<Person>> GetPersonsAsync()
        {
			return GetAllAsync<Person>("Person");
        }

		/// <summary>
		/// Creates a task that fetches all locations in the database.
		/// </summary>
		public Task<IEnumerable<Location>> GetLocationsAsync()
		{
			return GetAllAsync<Location>("Location");
		}

		/// <summary>
		/// Creates a task that fetches all person-location pairs
		/// from the has-location table.
		/// </summary>
		public Task<IEnumerable<PersonLocationPair>> GetPersonLocationMapAsync()
		{
			return GetAllAsync<PersonLocationPair>("HasLocation");
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
			return GetAllAsync<Sensor>("Sensor");
		}

        public void Dispose()
        {
            sqlite.Close();
        }
    }
}
