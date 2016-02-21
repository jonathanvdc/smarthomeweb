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

        public async Task<List<Person>> GetAllPersons()
        {
            var persons = await db.FetchAsync<Person>("SELECT * FROM Person");
            return persons;
        }

        public void Dispose()
        {
            sqlite.Close();
        }
    }
}
