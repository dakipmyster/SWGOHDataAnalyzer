using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using SWGOHInterface;

namespace SWGOHDBInterface
{
    public class DBInterface
    {
        #region Private Members

        private string m_folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer";
        private string m_dbTableName;
        private IEnumerable<string> m_dbList;
        private string m_dbName = "SWGOH.db";
        #endregion

        #region Public Members

        public bool HasOldSnapshots => m_dbList.Count() > 0;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbName">Name of the db table the user wants</param>
        public DBInterface(string dbTableName)
        {
            if (!Directory.Exists(m_folderPath))
                Directory.CreateDirectory(m_folderPath);

            if (!File.Exists($"{m_folderPath}\\SWGOH.db"))
                SQLiteConnection.CreateFile($"{m_folderPath}\\{m_dbName}");

            m_dbTableName = dbTableName;
            m_dbList = (Directory.EnumerateFiles(m_folderPath));
        }

        public void WriteDataToDB(Guild guild)
        {
            CreateTable();
            /*
            using (var conn = new SQLiteConnection($"{m_folderPath}\\{m_dbName}"))
            {
                conn.Open();
                
                using (var cmd = new SQLiteCommand(conn))
                {
                    using (var transaction = conn.BeginTransaction())
                    {
                        for (var i = 0; i < 1000000; i++)
                        {
                            cmd.CommandText =$"INSERT INTO {m_dbTableName} (FirstName, LastName) VALUES ('John', 'Doe');";
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                conn.Close();
            }*/
        }

        private void CreateTable()
        {
            string sql = "create table highscores (name varchar(20), score int)";

            using (SQLiteConnection m_dbConnection = new SQLiteConnection($"Data Source={m_folderPath}\\{m_dbName}"))
            {
                m_dbConnection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection)) 
                    command.ExecuteNonQuery();
            }
        }
    }
}
