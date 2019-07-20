using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using SWGOHInterface;
using SWGOHMessage;

namespace SWGOHDBInterface
{
    public class DBInterface
    {
        #region Private Members

        private string m_folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer";
        private string m_dbTableName;
        private List<string> m_tableList = new List<string>();
        private string m_dbName = "SWGOH.db";
        #endregion

        #region Public Members

        public bool HasOldSnapshots => m_tableList.Count() > 0;

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

            CollectTables();
        }

        private void CollectTables()
        {
            string sql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";

            using (SQLiteConnection m_dbConnection = new SQLiteConnection($"Data Source={m_folderPath}\\{m_dbName}"))
            {
                m_dbConnection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    command.CommandType = System.Data.CommandType.Text;
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            m_tableList.Add(reader.GetString(0));
                    }
                }
            }
        }

        public void WriteDataToDB(Guild guild)
        {
            CreateTable();

            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={m_folderPath}\\{m_dbName}"))
            {
                conn.Open();
                
                    Parallel.ForEach(guild.Players.AsEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (player) =>
                    {
                        SWGOHMessageSystem.OutputMessage($"Processing player {player.PlayerData.Name} for snapshot");

                        foreach (PlayerUnit unit in player.PlayerUnits)
                        {
                            using (var cmd = new SQLiteCommand(conn))
                            {
                                cmd.Parameters.AddRange(CollectSQLParams(player.PlayerData.Name, player.PlayerData.PlayerPower, unit));

                                //Yes, the SQL Injection again
                                cmd.CommandText = $@"INSERT INTO {m_dbTableName}
(player_name, player_power, toon, toon_power, toon_level, is_ship, gear_level, rarity, health, protection, speed, p_offense, s_offense, p_defense, s_defense, p_crit_chance, s_crit_chance, potency, tenacity, total_zetas, zeta_one, zeta_two, zeta_three) 
VALUES (@player_name, @player_power, @toon, @toon_power, @toon_level, @is_ship, @gear_level, @rarity, @health, @protection, @speed, @p_offense, @s_offense, @p_defense, @s_defense, @p_crit_chance, @s_cirt_chance, @potency, @tenacity, @total_zetas, @zeta_one, @zeta_two, @zeta_three) ;";


                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                
                conn.Close();
            }

            SWGOHMessageSystem.OutputMessage("Snapshot Complete!");
        }

        private SQLiteParameter[] CollectSQLParams(string playerName, int playerPower, PlayerUnit unit)
        {
            List<SQLiteParameter> sqlParams = new List<SQLiteParameter>();

            sqlParams.Add(new SQLiteParameter("@player_name", "Kyles Fart"));
            sqlParams.Add(new SQLiteParameter("@player_power", 1000000));
            sqlParams.Add(new SQLiteParameter("@toon", "Kyles Toon Fart"));
            sqlParams.Add(new SQLiteParameter("@toon_power", 12345));
            sqlParams.Add(new SQLiteParameter("@toon_level", 85));
            sqlParams.Add(new SQLiteParameter("@is_ship", 0));
            sqlParams.Add(new SQLiteParameter("@gear_level", 13));
            sqlParams.Add(new SQLiteParameter("@rarity", 7));
            sqlParams.Add(new SQLiteParameter("@health", 22222));
            sqlParams.Add(new SQLiteParameter("@protection", 22222));
            sqlParams.Add(new SQLiteParameter("@speed", 222));
            sqlParams.Add(new SQLiteParameter("@p_offense", 222));
            sqlParams.Add(new SQLiteParameter("@s_offense", 222));
            sqlParams.Add(new SQLiteParameter("@p_defense", 222));
            sqlParams.Add(new SQLiteParameter("@s_defense", 222));
            sqlParams.Add(new SQLiteParameter("@p_crit_chance", 222));
            sqlParams.Add(new SQLiteParameter("@s_cirt_chance", 222));
            sqlParams.Add(new SQLiteParameter("@potency", 222));
            sqlParams.Add(new SQLiteParameter("@tenacity", 222));
            sqlParams.Add(new SQLiteParameter("@total_zetas", 3));
            sqlParams.Add(new SQLiteParameter("@zeta_one", "Kyles Zeta Fart"));
            sqlParams.Add(new SQLiteParameter("@zeta_two", "Kyles Zetaer Fart"));
            sqlParams.Add(new SQLiteParameter("@zeta_three", "Kyles Zetaist Fart"));

            return sqlParams.ToArray();
        }

        /// <summary>
        /// Creates the new table based on the snapshot name the user wanted
        /// </summary>
        private void CreateTable()
        {            
            //Yes, I'm opening it to SQL Injection. Bite me. Unless future plans for this project means it is stored in a cloud based service you can only hurt yourself.
            string sql = $@"CREATE TABLE {m_dbTableName}
(
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    player_name varchar(30),
    player_power int,
    toon varchar(50),
    toon_power int,
    toon_level int,
    is_ship boolean,
    gear_level int,
    rarity int,
    health double,
    protection double,
    speed double,
    p_offense double,
    s_offense double,
    p_defense double,
    s_defense double,
    p_crit_chance double,
    s_crit_chance double,
    potency double,
    tenacity double,
    total_zetas int,
    zeta_one varchar(50),
    zeta_two varchar(50),
    zeta_three varchar(50)

)";

            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={m_folderPath}\\{m_dbName}"))
            {
                conn.Open();

                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
