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
            var unitMods = new List<Mod>();
            CreateTable();
            CreateModTable();

            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={m_folderPath}\\{m_dbName}"))
            {
                conn.Open();
                
                    Parallel.ForEach(guild.Players.AsEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (player) =>
                    {
                        SWGOHMessageSystem.OutputMessage($"Processing player {player.PlayerData.Name} for snapshot");

                        foreach (PlayerUnit unit in player.PlayerUnits)
                        {
                            lock (unitMods)
                                { unitMods.AddRange(unit.UnitData.UnitMods); }

                            using (var cmd = new SQLiteCommand(conn))
                            {
                                cmd.Parameters.AddRange(CollectSQLParams(player.PlayerData, guild.GuildData.GuildName, unit));

                                //Yes, the SQL Injection again
                                cmd.CommandText = $@"INSERT INTO {m_dbTableName}
(guild_name, player_name, ally_code, player_power, toon, toon_id, toon_power, toon_level, is_ship, gear_level, rarity, health, protection, speed, p_offense, s_offense, p_defense, s_defense, p_crit_chance, s_crit_chance, potency, tenacity, total_zetas, zeta_one, zeta_two, zeta_three, zeta_four, zeta_five, zeta_six, pull_date, relic_tier, gear_one_equipped, gear_two_equipped, gear_three_equipped, gear_four_equipped, gear_five_equipped, gear_six_equipped) 
VALUES (@guild_name, @player_name, @ally_code, @player_power, @toon, @toon_id, @toon_power, @toon_level, @is_ship, @gear_level, @rarity, @health, @protection, @speed, @p_offense, @s_offense, @p_defense, @s_defense, @p_crit_chance, @s_crit_chance, @potency, @tenacity, @total_zetas, @zeta_one, @zeta_two, @zeta_three, @zeta_four, @zeta_five, @zeta_six, @pull_date, @relic_tier, @gear_one_equipped, @gear_two_equipped, @gear_three_equipped, @gear_four_equipped, @gear_five_equipped, @gear_six_equipped) ;";


                                cmd.ExecuteNonQuery();
                            }
                        }
                    });

                    SWGOHMessageSystem.OutputMessage($"Processing guild mods");

                    Parallel.ForEach(unitMods.AsEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (unitMod) =>
                    {                        
                        using (var cmd = new SQLiteCommand(conn))
                        {
                            cmd.Parameters.AddRange(CollectSQLParamsForMods(unitMod));

                            //Yes, the SQL Injection again
                            cmd.CommandText = $@"INSERT INTO MOD_{m_dbTableName}
(player_id, toon_id, mod_set, mod_primary_name, mod_secondary_one_name, mod_secondary_one, mod_secondary_two_name, mod_secondary_two, mod_secondary_three_name, mod_secondary_three, mod_secondary_four_name, mod_secondary_four, mod_tier, mod_rarity) 
VALUES (@player_id, @toon_id, @mod_set, @mod_primary_name, @mod_secondary_one_name, @mod_secondary_one, @mod_secondary_two_name, @mod_secondary_two, @mod_secondary_three_name, @mod_secondary_three, @mod_secondary_four_name, @mod_secondary_four, @mod_tier, @mod_rarity);";


                            cmd.ExecuteNonQuery();
                        }
                        
                    });

                conn.Close();
            }

            SWGOHMessageSystem.OutputMessage("Snapshot Complete!");
        }

        /// <summary>
        /// Puts together the parameters for the sql insert
        /// </summary>
        /// <param name="modData">Mod data for character</param>
        /// <returns>An array of all the sql params needed to insert the data</returns>
        private SQLiteParameter[] CollectSQLParamsForMods(Mod modData)        
        {
            List<SQLiteParameter> sqlParams = new List<SQLiteParameter>();

            switch(modData.Set)
            {
                case "1":
                    modData.Set = "Health";
                    break;

                case "2":
                    modData.Set = "Offense";
                    break;

                case "3":
                    modData.Set = "Defense";
                    break;

                case "4":
                    modData.Set = "Speed";
                    break;

                case "5":
                    modData.Set = "Crit Chance";
                    break;

                case "6":
                    modData.Set = "Crit Damage";
                    break;

                case "7":
                    modData.Set = "Potency";
                    break;

                case "8":
                    modData.Set = "Tenacity";
                    break;
            }

            switch(modData.Tier)
            {
                case "1":
                    modData.Tier = "E";
                    break;

                case "2":
                    modData.Tier = "D";
                    break;

                case "3":
                    modData.Tier = "C";
                    break;

                case "4":
                    modData.Tier = "B";
                    break;

                case "5":
                    modData.Tier = "A";
                    break;
            }

            sqlParams.Add(new SQLiteParameter("@player_id", modData.PlayerId));
            sqlParams.Add(new SQLiteParameter("@toon_id", modData.ToonId));
            sqlParams.Add(new SQLiteParameter("@mod_set", modData.Set));
            sqlParams.Add(new SQLiteParameter("@mod_primary_name", modData.PrimaryModData.Name));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_one_name", modData.SecondaryStats.ElementAtOrDefault(0)?.Name));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_one", modData.SecondaryStats.ElementAtOrDefault(0)?.Value));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_two_name", modData.SecondaryStats.ElementAtOrDefault(1)?.Name));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_two", modData.SecondaryStats.ElementAtOrDefault(1)?.Value));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_three_name", modData.SecondaryStats.ElementAtOrDefault(2)?.Name));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_three", modData.SecondaryStats.ElementAtOrDefault(2)?.Value));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_four_name", modData.SecondaryStats.ElementAtOrDefault(3)?.Name));
            sqlParams.Add(new SQLiteParameter("@mod_secondary_four", modData.SecondaryStats.ElementAtOrDefault(3)?.Value));
            sqlParams.Add(new SQLiteParameter("@mod_tier", modData.Tier));
            sqlParams.Add(new SQLiteParameter("@mod_rarity", modData.Rarity));

            return sqlParams.ToArray();
        }

        /// <summary>
        /// Puts together the parameters for the sql insert
        /// </summary>
        /// <param name="playerData">Data relative to the player</param>
        /// <param name="guildName">Guild name</param>
        /// <param name="unit">Unit object of all the data</param>
        /// <returns>An array of all the sql params needed to insert the data</returns>
        private SQLiteParameter[] CollectSQLParams(PlayerData playerData, string guildName, PlayerUnit unit)
        {
            List<SQLiteParameter> sqlParams = new List<SQLiteParameter>();

            int isShip = unit.UnitData.Gear.Count > 0 ? 0 : 1;
            List<string> zetas = new List<string>();

            foreach (string zeta in unit.UnitData.AppliedZetas)
            {
                if (unit.UnitData.UnitAbilities.FirstOrDefault(a => a.AbilityId == zeta) != null)
                {
                    zetas.Add(unit.UnitData.UnitAbilities.FirstOrDefault(a => a.AbilityId == zeta).AbilityName);
                }
            }

            sqlParams.Add(new SQLiteParameter("@guild_name", guildName));
            sqlParams.Add(new SQLiteParameter("@player_name", playerData.Name));
            sqlParams.Add(new SQLiteParameter("@ally_code", playerData.AllyCode));
            sqlParams.Add(new SQLiteParameter("@player_power", playerData.PlayerPower));
            sqlParams.Add(new SQLiteParameter("@toon", unit.UnitData.Name));
            sqlParams.Add(new SQLiteParameter("@toon_id", unit.UnitData.UnitId));
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
            sqlParams.Add(new SQLiteParameter("@potency", Math.Round(unit.UnitData.UnitStats.Potency * 100, 2)));
            sqlParams.Add(new SQLiteParameter("@tenacity", Math.Round(unit.UnitData.UnitStats.Tenacity * 100, 2)));
            sqlParams.Add(new SQLiteParameter("@total_zetas", unit.UnitData.AppliedZetas.Count));
            sqlParams.Add(new SQLiteParameter("@zeta_one", zetas.ElementAtOrDefault(0)));
            sqlParams.Add(new SQLiteParameter("@zeta_two", zetas.ElementAtOrDefault(1)));
            sqlParams.Add(new SQLiteParameter("@zeta_three", zetas.ElementAtOrDefault(2)));
            sqlParams.Add(new SQLiteParameter("@zeta_four", zetas.ElementAtOrDefault(3)));
            sqlParams.Add(new SQLiteParameter("@zeta_five", zetas.ElementAtOrDefault(4)));
            sqlParams.Add(new SQLiteParameter("@zeta_six", zetas.ElementAtOrDefault(5)));
            sqlParams.Add(new SQLiteParameter("@pull_date", DateTime.Now));
            sqlParams.Add(new SQLiteParameter("@relic_tier", unit.UnitData.RelicTier >= 3 ? unit.UnitData.RelicTier - 2 : 0));
            sqlParams.Add(new SQLiteParameter("@gear_one_equipped", unit.UnitData.Gear.Count > 0 && unit.UnitData.Gear.FirstOrDefault(a => a.SlotPosition == 0).IsObtained ? 1 : 0));
            sqlParams.Add(new SQLiteParameter("@gear_two_equipped", unit.UnitData.Gear.Count > 0 && unit.UnitData.Gear.FirstOrDefault(a => a.SlotPosition == 1).IsObtained ? 1 : 0));
            sqlParams.Add(new SQLiteParameter("@gear_three_equipped", unit.UnitData.Gear.Count > 0 && unit.UnitData.Gear.FirstOrDefault(a => a.SlotPosition == 2).IsObtained ? 1 : 0));
            sqlParams.Add(new SQLiteParameter("@gear_four_equipped", unit.UnitData.Gear.Count > 0 && unit.UnitData.Gear.FirstOrDefault(a => a.SlotPosition == 3).IsObtained ? 1 : 0));
            sqlParams.Add(new SQLiteParameter("@gear_five_equipped", unit.UnitData.Gear.Count > 0 && unit.UnitData.Gear.FirstOrDefault(a => a.SlotPosition == 4).IsObtained ? 1 : 0));
            sqlParams.Add(new SQLiteParameter("@gear_six_equipped", unit.UnitData.Gear.Count > 0 && unit.UnitData.Gear.FirstOrDefault(a => a.SlotPosition == 5).IsObtained ? 1 : 0));

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
    ally_code int,
    player_power int,
    toon varchar(50),
    toon_id varchar(50),
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
    zeta_four varchar(50),
    zeta_five varchar(50),
    zeta_six varchar(50),
    pull_date date,
    relic_tier int,
    gear_one_equipped int,
    gear_two_equipped int,
    gear_three_equipped int,
    gear_four_equipped int,
    gear_five_equipped int,
    gear_six_equipped int
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
        /// Creates the new mod table based on the snapshot name the user wanted
        /// </summary>
        private void CreateModTable()
        {
            //Yes, I'm opening it to SQL Injection. Bite me. Unless future plans for this project means it is stored in a cloud based service you can only hurt yourself.
            string sql = $@"CREATE TABLE MOD_{m_dbTableName}
(
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    player_id int,
    toon_id varchar(50),
    mod_set varchar(20),
    mod_primary_name varchar(20),
    mod_secondary_one_name varchar(20),
    mod_secondary_one double,
    mod_secondary_two_name varchar(20),
    mod_secondary_two double,
    mod_secondary_three_name varchar(20),
    mod_secondary_three double,
    mod_secondary_four_name varchar(20),
    mod_secondary_four double,
    mod_tier varchar (2),
    mod_rarity
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
