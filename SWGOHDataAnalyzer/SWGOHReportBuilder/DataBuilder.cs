using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using SWGOHDBInterface;
using SWGOHMessage;

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

        public DataBuilder()
        {
            m_dbInterface = new DBInterface();
            PlayerData = new List<PlayerData>();
            UnitData = new List<UnitData>();
            ShipData = new List<ShipData>();
        }

        public bool CanRunReport()
        {
            if (!m_dbInterface.HasOldSnapshots)
            {
                SWGOHMessageSystem.OutputMessage("Not enough snapshots have been made, need at least 2 snapshots to create a report.");
                return false;
            }

            return true;
        }

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

        internal async Task CollectPlayerGPDifferences()
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
        
        internal async Task CollectShipData()
        {
            string sqlQuery = $@"SELECT player_name, toon, rarity, player_power, 'New' as 'State'
FROM {m_newSnapshot} WHERE is_ship = 1
UNION
SELECT player_name, toon, rarity, player_power, 'Old' as 'State'
FROM {m_oldSnapshot} WHERE is_ship = 1";

            DataTable results = await m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);

            foreach (DataRow row in results.Rows)
            {
                string playerName = row["player_name"].ToString();
                string state = row["State"].ToString();
                string shipName = row["toon"].ToString();
                int rarity = Convert.ToInt32(row["rarity"].ToString());
                int power = Convert.ToInt32(row["player_power"].ToString());

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
                }
                else
                {
                    ship.OldRarity = rarity;
                    ship.OldGalaticPower = power;
                }
            }

            ShipData = ShipData.Where(a => a.OldGalaticPower != 0 && a.NewGalaticPower != 0).ToList();
        }

        internal async Task CollectUnitData()
        {
            string sqlQuery = $@"SELECT player_name, toon, rarity, player_power, gear_level, toon_power, toon_level, health, protection, speed, p_offense,
s_offense, p_defense, s_defense, p_crit_chance, s_crit_chance, potency, tenacity, zeta_one, zeta_two, zeta_three, 'New' as 'State'
FROM {m_newSnapshot} WHERE is_ship = 0
UNION
SELECT player_name, toon, rarity, player_power, gear_level, toon_power, toon_level, health, protection, speed, p_offense,
s_offense, p_defense, s_defense, p_crit_chance, s_crit_chance, potency, tenacity, zeta_one, zeta_two, zeta_three, 'Old' as 'State'
FROM {m_oldSnapshot} WHERE is_ship = 0";

            DataTable results = await m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);

            foreach (DataRow row in results.Rows)
            {
                string playerName = row["player_name"].ToString();
                string state = row["State"].ToString();
                string unitName = row["toon"].ToString();
                int rarity = Convert.ToInt32(row["rarity"].ToString());
                int playerPower = Convert.ToInt32(row["player_power"].ToString());
                int unitPower = Convert.ToInt32(row["toon_power"].ToString());
                int unitLevel = Convert.ToInt32(row["toon_level"].ToString());
                int gearLevel = Convert.ToInt32(row["gear_level"].ToString());

                List<string> zetas = new List<string>();
                if (!String.IsNullOrEmpty(row["zeta_one"].ToString()))
                    zetas.Add(row["zeta_one"].ToString());

                if (!String.IsNullOrEmpty(row["zeta_two"].ToString()))
                    zetas.Add(row["zeta_two"].ToString());

                if (!String.IsNullOrEmpty(row["zeta_three"].ToString()))
                    zetas.Add(row["zeta_three"].ToString());

                UnitData unit;

                if (UnitData.Exists(a => a.PlayerName == playerName && a.UnitName == unitName))
                    unit = UnitData.First(a => a.PlayerName == playerName && a.UnitName == unitName);
                else
                {
                    unit = new UnitData();
                    unit.PlayerName = playerName;
                    unit.UnitName = unitName;
                    UnitData.Add(unit);
                }

                if (state == "New")
                {
                    unit.NewRarity = rarity;
                    unit.NewGalaticPower = playerPower;
                    unit.NewZetas = zetas;
                    unit.NewGearLevel = gearLevel;
                    unit.NewPower = unitPower;
                    unit.NewLevel = unitLevel;
                    unit.CurrentHealth = Convert.ToDecimal(row["health"].ToString());
                    unit.CurrentProtection = Convert.ToDecimal(row["protection"].ToString());
                    unit.CurrentSpeed = Convert.ToDecimal(row["speed"].ToString());
                    unit.CurrentPhysicalOffense = Convert.ToDecimal(row["p_offense"].ToString());
                    unit.CurrentSpecialOffense = Convert.ToDecimal(row["s_offense"].ToString());
                    unit.CurrentPhysicalDefense = Convert.ToDecimal(row["p_defense"].ToString());
                    unit.CurrentSpecialDefense = Convert.ToDecimal(row["s_defense"].ToString());
                    unit.CurrentPhysicalCritChance = Convert.ToDecimal(row["p_crit_chance"].ToString());
                    unit.CurrentSpecialCritChance = Convert.ToDecimal(row["s_crit_chance"].ToString());
                    unit.CurrentPotency = Convert.ToDecimal(row["potency"].ToString());
                    unit.CurrentTenacity = Convert.ToDecimal(row["tenacity"].ToString());
                }
                else
                {
                    unit.OldRarity = rarity;
                    unit.OldGalaticPower = playerPower;
                    unit.OldZetas = zetas;
                    unit.OldGearLevel = gearLevel;
                    unit.OldPower = unitPower;
                    unit.OldLevel = unitLevel;
                }
            }

            UnitData = UnitData.Where(a => a.OldGalaticPower != 0 && a.NewGalaticPower != 0).ToList();

            foreach (UnitData unit in UnitData)
                unit.PowerDifference = unit.NewPower - unit.OldPower;
        }
    }
}
