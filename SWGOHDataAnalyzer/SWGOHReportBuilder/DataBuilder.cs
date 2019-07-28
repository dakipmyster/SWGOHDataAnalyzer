using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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

        public DataBuilder()
        {
            m_dbInterface = new DBInterface();
            PlayerData = new List<PlayerData>();
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

            DataTable results = m_dbInterface.ExecuteQueryAndReturnResults(sqlQuery, null);
                        
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

        
    }
}
