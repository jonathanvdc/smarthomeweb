using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using AsyncPoco;

namespace SmartHomeWeb
{
    public class DataConnection : IDisposable
    {
        const string DatabasePath = "../../../../database/smarthomeweb.db";
        const string ConnectionString = "Data Source=" + DatabasePath;
        private SQLiteConnection sqlite;
        private Database db;

        public DataConnection()
        {
            sqlite = new SQLiteConnection(ConnectionString);
            sqlite.Open();
            db = new Database(sqlite);
        }

        public async Task Testy()
        {
            var count = await db.ExecuteScalarAsync<long>("SELECT count(*) FROM Person");
            Console.WriteLine(count);
        }

        public void Dispose()
        {
            sqlite.Close();
        }
    }
}