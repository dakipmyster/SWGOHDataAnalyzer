using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWGOHMessage;
using SWGOHInterface;
using SWGOHDBInterface;
using Newtonsoft.Json;
using System.IO;


namespace SWGOHReportBuilder
{
    public class DataBuilder
    {
        private string m_folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer";
        private string m_oldSnapshotName;
        private string m_newSnapshotName;

        internal GuildDifference DifferencesGuildData { get; private set; }
        internal Guild NewGuildData { get; private set; }
        private Guild OldGuildData { get; set; }

        private List<string> Snapshots { get; set; }

        public DataBuilder()
        {
            Snapshots = Directory.GetFiles(m_folderPath, "*.json").ToList();
            DifferencesGuildData = new GuildDifference();
        }

        /// <summary>
        /// Gets all of the names of the snapshots in the database
        /// </summary>
        public void GetSnapshotNames()
        {
            SWGOHMessageSystem.OutputMessage($"Here is the list of all available snapshots \r\n{String.Join("\r\n", Snapshots.ToArray())} \r\n");

            m_oldSnapshotName = SWGOHMessageSystem.InputMessage("Enter in the name of the older snapshot");
            m_newSnapshotName = SWGOHMessageSystem.InputMessage("Enter in the name of the newer snapshot");

            if (!Snapshots.Contains(m_oldSnapshotName) || !Snapshots.Contains(m_newSnapshotName))
            {
                SWGOHMessageSystem.OutputMessage("Entered snapshot name did not match ones available \r\n");
                GetSnapshotNames();
            }
        }

        /// <summary>
        /// Gets the metadata info for the snapshot
        /// </summary>
        /// <returns></returns>
        internal async Task GetGuildData()
        {
            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText(m_oldSnapshotName))
            {
                JsonSerializer serializer = new JsonSerializer();
                OldGuildData = (Guild)serializer.Deserialize(file, typeof(Guild));
            }

            using (StreamReader file = File.OpenText(m_newSnapshotName))
            {
                JsonSerializer serializer = new JsonSerializer();
                NewGuildData = (Guild)serializer.Deserialize(file, typeof(Guild));
            }

            BuildComparisonData();
        }

        private void BuildComparisonData()
        {
            DifferencesGuildData.OldGP = OldGuildData.Players.Sum(x => x.PlayerData.PlayerPower);
            DifferencesGuildData.NewGP = NewGuildData.Players.Sum(x => x.PlayerData.PlayerPower);

            foreach(var oldPlayerData in OldGuildData.Players)
            {
                var newPlayerData = NewGuildData.Players.Single(x => x.PlayerData.AllyCode == oldPlayerData.PlayerData.AllyCode);
                if (newPlayerData != null)
                    DifferencesGuildData.Players.Add(CollectPlayerDifferences(oldPlayerData, newPlayerData));
            }
        }

        private PlayerDifference CollectPlayerDifferences(Player oldPlayerData, Player newPlayerData)
        {
            var playerDifference = new PlayerDifference()
            {
                NewGP = newPlayerData.PlayerData.PlayerPower,
                OldGP = oldPlayerData.PlayerData.PlayerPower,
                AllyCode = newPlayerData.PlayerData.AllyCode,
                Name = newPlayerData.PlayerData.Name
            };            

            Parallel.ForEach(newPlayerData.PlayerUnits.AsEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (newUnit) =>
            {
                //Kind of like a list of domain events but really stupid simple
                List<bool> differencesDetected = new List<bool>();
                
                var oldUnit = oldPlayerData.PlayerUnits.SingleOrDefault(x => x.UnitData.UnitId == newUnit.UnitData.UnitId);

                var unitDifference = new UnitDifference()
                {
                    IsShip = newUnit.UnitData.GearLevel == 0 ? true : false,
                    Name = newUnit.UnitData.Name
                };

                differencesDetected.Add(IsUnitGPDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsRarityDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsRelicTierDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsGearLevelDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsZetaDifferences(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsOmicronDifferences(oldUnit, newUnit, unitDifference));

                lock (playerDifference)
                    playerDifference.Units.Add(unitDifference);                
            });

            return playerDifference;
        }

        private bool IsOmicronDifferences(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            return false;
        }

        private bool IsZetaDifferences(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            return false;
        }

        private bool IsGearLevelDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            return false;
        }

        private bool IsRelicTierDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            return false;
        }

        private bool IsRarityDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            return false;
        }

        private bool IsUnitGPDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if(oldUnit == null || oldUnit.UnitData.Power < newUnit.UnitData.Power)
            {
                unitDifference.OldGP = oldUnit != null ? oldUnit.UnitData.Power : 0;
                unitDifference.NewGP = newUnit.UnitData.Power;
                return true;
            }

            return false;
        }
    }
}
