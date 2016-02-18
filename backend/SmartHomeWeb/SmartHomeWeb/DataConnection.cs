using System.Data;
using System.Data.SQLite;

namespace SmartHomeWeb
{
    public class DataConnection
    {
        const string DatabasePath = "../database/smarthomeweb.db";
        const string ConnectionString = "Data Source=" + DatabasePath;
        private SQLiteConnection sqlite;

        public DataConnection()
        {
            sqlite = new SQLiteConnection(ConnectionString);
        }
    }
}