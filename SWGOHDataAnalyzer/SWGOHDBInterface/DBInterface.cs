using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using SWGOHInterface;
using SWGOHMessage;
using System.Data;

namespace SWGOHDBInterface
{
    public class DBInterface
    {
        #region Private Members

        private string m_folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer";
        private string m_dbTableName;
        private string m_dbName = "SWGOH.db";
        #endregion

        #region Public Members

        public bool HasOldSnapshots => Tables.Count() > 1;

        public List<string> Tables { get; } = new List<string>();

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

        public DBInterface()
        {
            CollectTables();
        }

        /// <summary>
        /// Method to collect the name of all the tables in the system as snapshot names
        /// </summary>
        private void CollectTables()
        {
            string sql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";

            using (SQLiteConnection m_dbConnection = new SQLiteConnection($"Data Source={m_folderPath}\\{m_dbName}"))
            {
                m_dbConnection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    command.CommandType = CommandType.Text;
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            Tables.Add(reader.GetString(0));
                    }
                }
            }
        }

        /// <summary>
        /// Collects all of the data from the interface data pull and inserts it into the snapshot database
        /// </summary>
        /// <param name="guild"></param>
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
                                cmd.Parameters.AddRange(CollectSQLParams(player.PlayerData.Name, player.PlayerData.PlayerPower, guild.GuildData.GuildName, unit));

                                //Yes, the SQL Injection again
                                cmd.CommandText = $@"INSERT INTO {m_dbTableName}
(guild_name, player_name, player_power, toon, toon_power, toon_level, is_ship, gear_level, rarity, health, protection, speed, p_offense, s_offense, p_defense, s_defense, p_crit_chance, s_crit_chance, potency, tenacity, total_zetas, zeta_one, zeta_two, zeta_three, pull_date, relic_tier) 
VALUES (@guild_name, @player_name, @player_power, @toon, @toon_power, @toon_level, @is_ship, @gear_level, @rarity, @health, @protection, @speed, @p_offense, @s_offense, @p_defense, @s_defense, @p_crit_chance, @s_crit_chance, @potency, @tenacity, @total_zetas, @zeta_one, @zeta_two, @zeta_three, @pull_date, @relic_tier) ;";


                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                
                conn.Close();
            }

            SWGOHMessageSystem.OutputMessage("Snapshot Complete!");
        }

        /// <summary>
        /// Puts together the parameters for the sql insert
        /// </summary>
        /// <param name="playerName">Name of the player</param>
        /// <param name="playerPower">Power of the player</param>
        /// <param name="guildName">Guild name</param>
        /// <param name="unit">Unit object of all the data</param>
        /// <returns>An array of all the sql params needed to insert the data</returns>
        private SQLiteParameter[] CollectSQLParams(string playerName, int playerPower, string guildName, PlayerUnit unit)
        {
            List<SQLiteParameter> sqlParams = new List<SQLiteParameter>();

            int isShip = unit.UnitData.Gear.Count > 0 ? 0 : 1;
            List<string> zetas = new List<string>();

            foreach(string zeta in unit.UnitData.AppliedZetas)
            {
                if(unit.UnitData.UnitAbilities.FirstOrDefault(a => a.AbilityId == zeta) != null)
                {
                    zetas.Add(unit.UnitData.UnitAbilities.FirstOrDefault(a => a.AbilityId == zeta).AbilityName);
                }
            }

            sqlParams.Add(new SQLiteParameter("@guild_name", guildName));
            sqlParams.Add(new SQLiteParameter("@player_name", playerName));
            sqlParams.Add(new SQLiteParameter("@player_power", playerPower));
            sqlParams.Add(new SQLiteParameter("@toon", unit.UnitData.Name));
            sqlParams.Add(new SQLiteParameter("@toon_power", unit.UnitData.Power));
            sqlParams.Add(new SQLiteParameter("@toon_level", unit.UnitData.Level));
            sqlParams.Add(new SQLiteParameter("@is_ship", isShip));
            sqlParams.Add(new SQLiteParameter("@gear_level", unit.UnitData.GearLevel));
            sqlParams.Add(new SQLiteParameter("@rarity", unit.UnitData.Rarity));
            sqlParams.Add(new SQLiteParameter("@health", unit.UnitData.UnitStats.Health));
            sqlParams.Add(new SQLiteParameter("@protection", unit.UnitData.UnitStats.Protection));
            sqlParams.Add(new SQLiteParameter("@speed", unit.UnitData.UnitStats.Speed));
            sqlParams.Add(new SQLiteParameter("@p_offense", unit.UnitData.UnitStats.PhysicalOffense));
            sqlParams.Add(new SQLiteParameter("@s_offense", unit.UnitData.UnitStats.SpecialOffense));
            sqlParams.Add(new SQLiteParameter("@p_defense", Math.Round(unit.UnitData.UnitStats.PhysicalDefense, 2)));
            sqlParams.Add(new SQLiteParameter("@s_defense", Math.Round(unit.UnitData.UnitStats.SpeicalDefense, 2)));
            sqlParams.Add(new SQLiteParameter("@p_crit_chance", Math.Round(unit.UnitData.UnitStats.PhysicalCriticalChance, 2)));
            sqlParams.Add(new SQLiteParameter("@s_crit_chance", Math.Round(unit.UnitData.UnitStats.SpecialCriticalChance, 2)));
            sqlParams.Add(new SQLiteParameter("@potency", Math.Round(unit.UnitData.UnitStats.Potency*100, 2)));
            sqlParams.Add(new SQLiteParameter("@tenacity", Math.Round(unit.UnitData.UnitStats.Tenacity*100, 2)));
            sqlParams.Add(new SQLiteParameter("@total_zetas", unit.UnitData.AppliedZetas.Count));
            sqlParams.Add(new SQLiteParameter("@zeta_one", zetas.ElementAtOrDefault(0)));
            sqlParams.Add(new SQLiteParameter("@zeta_two", zetas.ElementAtOrDefault(1)));
            sqlParams.Add(new SQLiteParameter("@zeta_three", zetas.ElementAtOrDefault(2)));
            sqlParams.Add(new SQLiteParameter("@pull_date", DateTime.Now));
            sqlParams.Add(new SQLiteParameter("@relic_tier", unit.UnitData.RelicTier >= 3 ? unit.UnitData.RelicTier - 2 : 0));

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
    guild_name varchar(40),
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
    zeta_three varchar(50),
    pull_date date,
    relic_tier int

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

        /// <summary>
        /// Executes  the sql and parameters passsed in
        /// </summary>
        /// <param name="sql">Sql query to run</param>
        /// <param name="parameters">Parameters for the query</param>
        /// <returns>Results of the query</returns>
        public async Task<DataTable> ExecuteQueryAndReturnResults(string sql, SQLiteParameter[] parameters)
        {
            DataTable table = new DataTable();

            using (SQLiteConnection m_dbConnection = new SQLiteConnection($"Data Source={m_folderPath}\\{m_dbName}"))
            {
                m_dbConnection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    command.CommandType = CommandType.Text;

                    if(parameters != null)
                        command.Parameters.AddRange(parameters);
                    
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        table.Load(reader);
                    }
                }
            }
                        
            return await Task.FromResult(table);
        }
    }
}
