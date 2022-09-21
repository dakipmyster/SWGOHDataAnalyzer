using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using SWGOHDBInterface;
using SWGOHMessage;
using SWGOHInterface;
using System.Data.SQLite;

namespace SWGOHReportBuilder
{
    public class DataBuilder
    {
        private DBInterface m_dbInterface;
        private string m_oldSnapshot;
        private string m_newSnapshot;

        internal List<PlayerData> PlayerData { get; set; }
        internal List<UnitData> UnitData { get; set; }
        internal List<ShipData> ShipData { get; set; }
        internal string GuildName { get; set; }
        internal string DateRange { get; set; }

        public DataBuilder()
        {
            m_dbInterface = new DBInterface();
            PlayerData = new List<PlayerData>();
            UnitData = new List<UnitData>();
            ShipData = new List<ShipData>();
        }

        /// <summary>
        /// Determines if there is enough snapshots in the database to run the detailed report
        /// </summary>
        /// <returns>True if two or more snapshots, otherwise false</returns>
        public bool CanRunReport()
        {
            if (!m_dbInterface.HasOldSnapshots)
            {
                SWGOHMessageSystem.OutputMessage("Not enough snapshots have been made, need at least 2 snapshots to create a report.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets all of the names of the snapshots in the database
        /// </summary>
        public void GetSnapshotNames()
        {
            SWGOHMessageSystem.OutputMessage($"Here is the list of all available snapshots \r\n{String.Join("\r\n", m_dbInterface.Tables.ToArray())} \r\n");

            string oldSnapshotName = SWGOHMessageSystem.InputMessage("Enter in the name of the older snapshot");
            string newSnapshotName = SWGOHMessageSystem.InputMessage("Enter in the name of the newer snapshot");

            if (m_dbInterface.Tables.Contains(oldSnapshotName) && m_dbInterface.Tables.Contains(newSnapshotName))
            {
                m_oldSnapshot = oldSnapshotName;
                m_newSnapshot = newSnapshotName;
            }
            else
            {
                SWGOHMessageSystem.OutputMessage("Entered snapshot name did not match ones available \r\n");
                GetSnapshotNames();
            }
        }

        /// <summary>
        /// Gets the metadata info for the snapshot
        /// </summary>
        /// <returns></returns>
        internal async Task CollectSnapshotMetadataFromDB()
        {
            string sqlQuery = $"SELECT DISTINCT guild_name, pull_date FROM {m_newSnapshot} LIMIT 1";

            DataTable newResults = await m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);

            foreach (DataRow row in newResults.Rows)
            {
                GuildName = row["guild_name"].ToString();
                DateRange = Convert.ToDateTime(row["pull_date"].ToString()).ToString("d");
            }

            sqlQuery = $"SELECT DISTINCT pull_date FROM {m_oldSnapshot} LIMIT 1";

            DataTable oldResults = await m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);

            foreach (DataRow row in oldResults.Rows)
            {
                DateRange = $"{Convert.ToDateTime(row["pull_date"].ToString()).ToString("d")} - {DateRange}";
            }
        }

        /// <summary>
        /// Collects all player related data from the snapshots
        /// </summary>
        /// <returns></returns>
        internal async Task CollectPlayerGPDifferencesFromDB()
        {
            string sqlQuery = $@"SELECT DISTINCT player_name, player_power, 'New' as 'State'
FROM {m_newSnapshot}
UNION
SELECT DISTINCT player_name, player_power, 'Old' as 'State'
FROM {m_oldSnapshot}";

            DataTable results = await m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);
                        
            foreach(DataRow row in results.Rows)
            {
                string playerName = row["player_name"].ToString();
                string state = row["State"].ToString();
                int power = Convert.ToInt32(row["player_power"].ToString());

                PlayerData player;

                if (PlayerData.Exists(a => a.PlayerName == playerName))
                    player = PlayerData.First(a => a.PlayerName == playerName);
                else
                {
                    player = new PlayerData();
                    player.PlayerName = playerName;
                    PlayerData.Add(player);
                }

                if (state == "New")
                    player.NewGalaticPower = power;
                else
                    player.OldGalaticPower = power;
            }

            PlayerData = PlayerData.Where(a => a.OldGalaticPower != 0 && a.NewGalaticPower != 0).ToList();

            foreach(PlayerData player in PlayerData)
            {
                player.GalaticPowerDifference = player.NewGalaticPower - player.OldGalaticPower;
                player.GalaticPowerPercentageDifference = Math.Round(Decimal.Divide(player.GalaticPowerDifference, player.OldGalaticPower) * 100, 3);
            }
        }
        
        /// <summary>
        /// Collects all of the ship data from the snapshots
        /// </summary>
        /// <returns></returns>
        internal async Task CollectShipDataFromDB()
        {
            string sqlQuery = $@"SELECT player_name, toon, toon_power, rarity, player_power, 'New' as 'State'
FROM {m_newSnapshot} WHERE is_ship = 1
UNION
SELECT player_name, toon, toon_power, rarity, player_power, 'Old' as 'State'
FROM {m_oldSnapshot} WHERE is_ship = 1";

            DataTable results = await m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);

            foreach (DataRow row in results.Rows)
            {
                string playerName = row["player_name"].ToString();
                string state = row["State"].ToString();
                string shipName = row["toon"].ToString();
                int rarity = Convert.ToInt32(row["rarity"].ToString());
                int power = Convert.ToInt32(row["player_power"].ToString());
                int shipPower = Convert.ToInt32(row["toon_power"].ToString());

                ShipData ship;

                if (ShipData.Exists(a => a.PlayerName == playerName && a.ShipName == shipName))
                    ship = ShipData.First(a => a.PlayerName == playerName && a.ShipName == shipName);
                else
                {
                    ship = new ShipData();
                    ship.PlayerName = playerName;
                    ship.ShipName = shipName;
                    ShipData.Add(ship);
                }

                if (state == "New")
                {
                    ship.NewRarity = rarity;
                    ship.NewGalaticPower = power;
                    ship.NewPower = shipPower;
                }
                else
                {
                    ship.OldRarity = rarity;
                    ship.OldGalaticPower = power;
                    ship.OldPower = power;
                }
            }
        }

        /// <summary>
        /// Collects all the unit data from the snapshots
        /// </summary>
        /// <returns></returns>
        internal async Task CollectUnitDataFromDB()
        {
            string sqlQuery = $@"SELECT player_name, toon, rarity, player_power, gear_level, toon_power, toon_level, health, protection, speed, p_offense, toon_id, ally_code,
s_offense, p_defense, s_defense, p_crit_chance, s_crit_chance, potency, tenacity, zeta_one, zeta_two, zeta_three, zeta_four, zeta_five, zeta_six, total_omicrons, omicron_one, omicron_two, omicron_three, omicron_four, omicron_five, omicron_six, relic_tier, gear_one_equipped, gear_two_equipped, gear_three_equipped, gear_four_equipped, gear_five_equipped, gear_six_equipped, 'New' as 'State'
FROM {m_newSnapshot} WHERE is_ship = 0
UNION
SELECT player_name, toon, rarity, player_power, gear_level, toon_power, toon_level, health, protection, speed, p_offense, toon_id, ally_code,
s_offense, p_defense, s_defense, p_crit_chance, s_crit_chance, potency, tenacity, zeta_one, zeta_two, zeta_three, zeta_four, zeta_five, zeta_six, total_omicrons, omicron_one, omicron_two, omicron_three, omicron_four, omicron_five, omicron_six, relic_tier, gear_one_equipped, gear_two_equipped, gear_three_equipped, gear_four_equipped, gear_five_equipped, gear_six_equipped, 'Old' as 'State'
FROM {m_oldSnapshot} WHERE is_ship = 0";

            DataTable results = await m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);

            Parallel.ForEach(results.Rows.OfType<DataRow>().AsEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (row) =>
            {
                string playerName = row["player_name"].ToString();
                string state = row["State"].ToString();
                string unitName = row["toon"].ToString();
                int rarity = Convert.ToInt32(row["rarity"].ToString());
                int playerPower = Convert.ToInt32(row["player_power"].ToString());
                int unitPower = Convert.ToInt32(row["toon_power"].ToString());
                int unitLevel = Convert.ToInt32(row["toon_level"].ToString());
                int gearLevel = Convert.ToInt32(row["gear_level"].ToString());
                int relicTier = Convert.ToInt32(row["relic_tier"].ToString());

                List<string> zetas = new List<string>();
                if (!String.IsNullOrEmpty(row["zeta_one"].ToString()))
                    zetas.Add(row["zeta_one"].ToString());

                if (!String.IsNullOrEmpty(row["zeta_two"].ToString()))
                    zetas.Add(row["zeta_two"].ToString());

                if (!String.IsNullOrEmpty(row["zeta_three"].ToString()))
                    zetas.Add(row["zeta_three"].ToString());

                if (!String.IsNullOrEmpty(row["zeta_four"].ToString()))
                    zetas.Add(row["zeta_four"].ToString());

                if (!String.IsNullOrEmpty(row["zeta_five"].ToString()))
                    zetas.Add(row["zeta_five"].ToString());

                if (!String.IsNullOrEmpty(row["zeta_six"].ToString()))
                    zetas.Add(row["zeta_six"].ToString());

                List<string> omicrons = new List<string>();
                if (!String.IsNullOrEmpty(row["omicron_one"].ToString()))
                    omicrons.Add(row["omicron_one"].ToString());

                if (!String.IsNullOrEmpty(row["omicron_two"].ToString()))
                    omicrons.Add(row["omicron_two"].ToString());

                if (!String.IsNullOrEmpty(row["omicron_three"].ToString()))
                    omicrons.Add(row["omicron_three"].ToString());

                if (!String.IsNullOrEmpty(row["omicron_four"].ToString()))
                    omicrons.Add(row["omicron_four"].ToString());

                if (!String.IsNullOrEmpty(row["omicron_five"].ToString()))
                    omicrons.Add(row["omicron_five"].ToString());

                if (!String.IsNullOrEmpty(row["omicron_six"].ToString()))
                    omicrons.Add(row["omicron_six"].ToString());

                UnitData unit;

                lock (UnitData)
                {
                    if (UnitData.Exists(a => a.PlayerName == playerName && a.UnitName == unitName))
                        unit = UnitData.First(a => a.PlayerName == playerName && a.UnitName == unitName);
                    else
                    {
                        unit = new UnitData()
                        {
                            PlayerName = playerName,
                            UnitName = unitName
                        };


                        UnitData.Add(unit);
                    }
                }

                if (state == "New")
                {
                    unit.NewRarity = rarity;
                    unit.NewGalaticPower = playerPower;
                    unit.NewZetas = zetas;
                    unit.NewOmicrons = omicrons;
                    unit.NewGearLevel = gearLevel;
                    unit.NewPower = unitPower;
                    unit.NewLevel = unitLevel;
                    unit.NewRelicTier = relicTier;
                    unit.CurrentHealth = Convert.ToDecimal(row["health"].ToString());
                    unit.CurrentProtection = Convert.ToDecimal(row["protection"].ToString());
                    unit.CurrentTankiest = unit.CurrentHealth + unit.CurrentProtection;
                    unit.CurrentSpeed = Convert.ToDecimal(row["speed"].ToString());
                    unit.CurrentPhysicalOffense = Convert.ToDecimal(row["p_offense"].ToString());
                    unit.CurrentSpecialOffense = Convert.ToDecimal(row["s_offense"].ToString());
                    unit.CurrentPhysicalDefense = Convert.ToDecimal(row["p_defense"].ToString());
                    unit.CurrentSpecialDefense = Convert.ToDecimal(row["s_defense"].ToString());
                    unit.CurrentPhysicalCritChance = Convert.ToDecimal(row["p_crit_chance"].ToString());
                    unit.CurrentSpecialCritChance = Convert.ToDecimal(row["s_crit_chance"].ToString());
                    unit.CurrentPotency = Convert.ToDecimal(row["potency"].ToString());
                    unit.CurrentTenacity = Convert.ToDecimal(row["tenacity"].ToString());
                    unit.HasGearSlotOne = Convert.ToInt32(row["gear_one_equipped"].ToString());
                    unit.HasGearSlotTwo = Convert.ToInt32(row["gear_two_equipped"].ToString());
                    unit.HasGearSlotThree = Convert.ToInt32(row["gear_three_equipped"].ToString());
                    unit.HasGearSlotFour = Convert.ToInt32(row["gear_four_equipped"].ToString());
                    unit.HasGearSlotFive = Convert.ToInt32(row["gear_five_equipped"].ToString());
                    unit.HasGearSlotSix = Convert.ToInt32(row["gear_six_equipped"].ToString());

                    var modSqlQuery = $@"SELECT id, mod_set, mod_primary_name, mod_secondary_one, mod_secondary_one_name, mod_secondary_two, mod_secondary_two_name, mod_secondary_three, 
mod_secondary_three_name, mod_secondary_four, mod_secondary_four_name, mod_tier, mod_rarity, mod_slot, mod_secondary_one_roll, mod_secondary_two_roll, mod_secondary_three_roll, mod_secondary_four_roll
FROM MOD_{m_newSnapshot} WHERE toon_id = @toonid AND player_id = @allycode";
                    var modSqlParams = new List<SQLiteParameter>(){
                        new SQLiteParameter() { ParameterName = "@allycode", Value = row["ally_code"].ToString() },
                        new SQLiteParameter() { ParameterName = "@toonid", Value = row["toon_id"].ToString() }
                    };

                    DataTable modResults = m_dbInterface.ExecuteQueryAndReturnResults(modSqlQuery, modSqlParams.ToArray()).GetAwaiter().GetResult();

                    foreach (DataRow modRow in modResults.Rows)
                    {
                        unit.Mods.Add(new Mod()
                        {
                            Id = Convert.ToInt32(modRow["id"].ToString()),
                            UnitName = unit.UnitName,
                            PlayerName = unit.PlayerName,
                            ModSet = modRow["mod_set"].ToString(),
                            ModPrimaryName = modRow["mod_primary_name"].ToString(),
                            ModRarity = $"{modRow["mod_rarity"]}{modRow["mod_tier"]}",
                            ModShape = modRow["mod_slot"].ToString(),
                            ModSecondaryOneName = modRow["mod_secondary_one_name"].ToString(),
                            ModSecondaryTwoName = modRow["mod_secondary_two_name"].ToString(),
                            ModSecondaryThreeName = modRow["mod_secondary_three_name"].ToString(),
                            ModSecondaryFourName = modRow["mod_secondary_four_name"].ToString(),
                            ModSecondaryOne = String.IsNullOrEmpty(modRow["mod_secondary_one"].ToString()) ? 0 : Convert.ToDecimal(modRow["mod_secondary_one"].ToString()),
                            ModSecondaryTwo = String.IsNullOrEmpty(modRow["mod_secondary_two"].ToString()) ? 0 : Convert.ToDecimal(modRow["mod_secondary_two"].ToString()),
                            ModSecondaryThree = String.IsNullOrEmpty(modRow["mod_secondary_three"].ToString()) ? 0 : Convert.ToDecimal(modRow["mod_secondary_three"].ToString()),
                            ModSecondaryFour = String.IsNullOrEmpty(modRow["mod_secondary_four"].ToString()) ? 0 : Convert.ToDecimal(modRow["mod_secondary_four"].ToString()),
                            ModSecondaryOneRoll = modRow["mod_secondary_one_roll"].ToString(),
                            ModSecondaryTwoRoll = modRow["mod_secondary_two_roll"].ToString(),
                            ModSecondaryThreeRoll = modRow["mod_secondary_three_roll"].ToString(),
                            ModSecondaryFourRoll = modRow["mod_secondary_four_roll"].ToString()

                        });
                    }
                }
                else
                {
                    unit.OldRarity = rarity;
                    unit.OldGalaticPower = playerPower;
                    unit.OldZetas = zetas;
                    unit.OldOmicrons = omicrons;
                    unit.OldGearLevel = gearLevel;
                    unit.OldPower = unitPower;
                    unit.OldLevel = unitLevel;
                    unit.OldRelicTier = relicTier;
                }
            });
            
            foreach (UnitData unit in UnitData)
                unit.PowerDifference = unit.NewPower - unit.OldPower;
        }

        /// <summary>
        /// Collects all the data from a recent interface pull
        /// </summary>
        /// <param name="guild">Guild data from the interface</param>
        /// <returns></returns>
        internal async Task CollectUnitDataFromInterface(Guild guild)
        {
            GuildName = guild.GuildData.GuildName;
            DateRange = DateTime.Now.ToString("d");
            var players = new List<Player>();
            foreach(Player player in players)
            {
                PlayerData.Add(new PlayerData() { PlayerName = player.PlayerData.Name, AllyCode = player.PlayerData.AllyCode });

                foreach (PlayerUnit unit in player.PlayerUnits)
                {
                    if (unit.UnitData.Gear.Count > 0)
                    {
                        List<string> zetas = new List<string>();

                        foreach (string zeta in unit.UnitData.AppliedZetas)
                        {
                            if (unit.UnitData.UnitAbilities.FirstOrDefault(a => a.AbilityId == zeta) != null)
                            {
                                zetas.Add(unit.UnitData.UnitAbilities.FirstOrDefault(a => a.AbilityId == zeta).AbilityName);
                            }
                        }

                        UnitData unitData = new UnitData()
                        {
                            PlayerName = player.PlayerData.Name,
                            CurrentHealth = (decimal)Math.Round(unit.UnitData.UnitStats.Health, 2),
                            CurrentPhysicalCritChance = (decimal)Math.Round(unit.UnitData.UnitStats.PhysicalCriticalChance, 2),
                            CurrentPhysicalDefense = (decimal)Math.Round(unit.UnitData.UnitStats.PhysicalDefense, 2),
                            CurrentPhysicalOffense = (decimal)Math.Round(unit.UnitData.UnitStats.PhysicalOffense, 2),
                            CurrentPotency = (decimal)Math.Round(unit.UnitData.UnitStats.Potency*100, 2),
                            CurrentProtection = (decimal)Math.Round(unit.UnitData.UnitStats.Protection, 2),
                            CurrentSpecialCritChance = (decimal)Math.Round(unit.UnitData.UnitStats.SpecialCriticalChance, 2),
                            CurrentSpecialDefense = (decimal)Math.Round(unit.UnitData.UnitStats.SpeicalDefense, 2),
                            CurrentSpecialOffense = (decimal)Math.Round(unit.UnitData.UnitStats.SpecialOffense, 2),
                            CurrentSpeed = (decimal)Math.Round(unit.UnitData.UnitStats.Speed, 2),
                            CurrentTankiest = (decimal)Math.Round(unit.UnitData.UnitStats.Health, 2) + (decimal)Math.Round(unit.UnitData.UnitStats.Protection, 2),
                            CurrentTenacity = (decimal)Math.Round(unit.UnitData.UnitStats.Tenacity*100, 2),
                            UnitName = unit.UnitData.Name,
                            UnitId = unit.UnitData.UnitId,
                            NewPower = unit.UnitData.Power,
                            NewGearLevel = unit.UnitData.GearLevel,
                            NewZetas = zetas,
                            NewRarity = unit.UnitData.Rarity,
                            NewRelicTier = unit.UnitData.RelicTier
                        };

                        UnitData.Add(unitData);
                        
                    }
                    else
                    {
                        ShipData.Add(new ShipData() { PlayerName = player.PlayerData.Name, NewPower = unit.UnitData.Power, NewRarity = unit.UnitData.Rarity, ShipName = unit.UnitData.Name });
                    }

                }
            }

            await Task.CompletedTask;
        }

    }
}
