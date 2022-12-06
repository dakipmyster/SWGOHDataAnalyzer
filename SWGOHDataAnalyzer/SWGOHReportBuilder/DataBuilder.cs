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
        internal Guild OldGuildData { get; set; }

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

            CleanData();
            BuildComparisonData();
        }

        private void CleanData()
        {
            NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.UnitStats.PhysicalDefense == Math.Round(y.UnitData.UnitStats.PhysicalDefense, 2));
            NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.UnitStats.SpeicalDefense == Math.Round(y.UnitData.UnitStats.SpeicalDefense, 2));
            NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.UnitStats.PhysicalCriticalChance == Math.Round(y.UnitData.UnitStats.PhysicalCriticalChance, 2));
            NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.UnitStats.SpecialCriticalChance == Math.Round(y.UnitData.UnitStats.SpecialCriticalChance, 2));
            NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.UnitStats.Potency == Math.Round(y.UnitData.UnitStats.Potency * 100, 2));
            NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.UnitStats.Tenacity == Math.Round(y.UnitData.UnitStats.Tenacity * 100, 2));
            NewGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.RelicTier = y.UnitData.RelicTier >= 3 ? y.UnitData.RelicTier - 2 : 0);
            OldGuildData.Players.SelectMany(x => x.PlayerUnits).Select(y => y.UnitData.RelicTier = y.UnitData.RelicTier >= 3 ? y.UnitData.RelicTier - 2 : 0);
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
                    UnitType = newUnit.UnitData.UnitType,
                    Name = newUnit.UnitData.Name,
                    PlayerName = newPlayerData.PlayerData.Name
                };

                differencesDetected.Add(IsUnitGPDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsRarityDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsRelicTierDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsGearLevelDifference(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsZetaDifferences(oldUnit, newUnit, unitDifference));
                differencesDetected.Add(IsOmicronDifferences(oldUnit, newUnit, unitDifference));

                if (differencesDetected.Any(x => x == true))
                {
                    lock (playerDifference)
                        playerDifference.Units.Add(unitDifference);
                }
            });

            return playerDifference;
        }

        private bool IsOmicronDifferences(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit.UnitData.AppliedOmicrons.Count() < newUnit.UnitData.AppliedOmicrons.Count())
            {
                foreach (var omicronName in newUnit.UnitData.AppliedOmicrons.Where(x => !oldUnit.UnitData.AppliedOmicrons.Contains(x)))
                    unitDifference.NewOmicrons.Add(omicronName);

                return true;
            }

            return false;
        }

        private bool IsZetaDifferences(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit.UnitData.AppliedZetas.Count() < newUnit.UnitData.AppliedZetas.Count())
            {
                foreach (var zetaName in newUnit.UnitData.AppliedZetas.Where(x => !oldUnit.UnitData.AppliedZetas.Contains(x)))
                    unitDifference.NewZetas.Add(zetaName);

                return true;
            }

            return false;
        }

        private bool IsGearLevelDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit.UnitData.GearLevel < newUnit.UnitData.GearLevel)
            {
                unitDifference.OldGearLevel = oldUnit != null ? oldUnit.UnitData.GearLevel : 0;
                unitDifference.NewGearLevel = newUnit.UnitData.GearLevel;
                return true;
            }

            return false;
        }

        private bool IsRelicTierDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit.UnitData.RelicTier < newUnit.UnitData.RelicTier)
            {
                unitDifference.OldRelicTier = oldUnit != null ? oldUnit.UnitData.RelicTier : 0;
                unitDifference.NewRelicTier = newUnit.UnitData.RelicTier;
                return true;
            }

            return false;
        }

        private bool IsRarityDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit.UnitData.Rarity < newUnit.UnitData.Rarity)
            {
                unitDifference.OldRarity = oldUnit != null ? oldUnit.UnitData.Rarity: 0;
                unitDifference.NewRarity = newUnit.UnitData.Rarity;
                return true;
            }

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
