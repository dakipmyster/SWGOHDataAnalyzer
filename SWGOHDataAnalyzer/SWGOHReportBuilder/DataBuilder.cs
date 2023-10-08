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
        private Dictionary<string, string> m_datacronNames => new Dictionary<string, string>()
        {
            { "datacron_set_4_base","Security Primer" },
            { "datacron_set_5_base","Projecting Power" },
            { "datacron_set_6_base","Dangerous Prototypes" },
            { "datacron_set_7_base","Defiance Rising" },
            { "datacron_set_8_base","Wandering Wisdom" },
            { "datacron_set_9_base","Archaic Rites" },
            { "datacron_set_10_base","Steadfast Resolve" },
            { "datacron_set_11_base","Arcane Visions Datacron" }
        };
        internal GuildDifference DifferencesGuildData { get; private set; }
        internal Guild NewGuildData { get; private set; }
        internal Guild OldGuildData { get; set; }

        private List<string> Snapshots { get; set; }

        public DataBuilder()
        {
            Snapshots = Directory.GetFiles(m_folderPath, "*.json")
                .Select(x => x.Replace($"{m_folderPath}\\", ""))
                .Select(x => x.Replace($".json", ""))
                .ToList();
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
            using (StreamReader file = File.OpenText($"{m_folderPath}\\{m_oldSnapshotName}.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                OldGuildData = (Guild)serializer.Deserialize(file, typeof(Guild));
                OldGuildData.Players = OldGuildData.Players.Where(x => x.PlayerData != null).ToList();
            }

            using (StreamReader file = File.OpenText($"{m_folderPath}\\{m_newSnapshotName}.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                NewGuildData = (Guild)serializer.Deserialize(file, typeof(Guild));
                NewGuildData.Players = NewGuildData.Players.Where(x => x.PlayerData != null).ToList();
            }

            PrettyUpData();
            BuildComparisonData();
        }

        private void PrettyUpData()
        {
            var unitDictionary = new Dictionary<string, string>();

            foreach(var playerUnit in NewGuildData.Players.SelectMany(x => x.PlayerUnits))
            {
                if (!unitDictionary.ContainsKey(playerUnit.UnitData.UnitId))
                    unitDictionary.Add(playerUnit.UnitData.UnitId, playerUnit.UnitData.Name);
            }

            Parallel.ForEach(NewGuildData.Players, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (player) =>
            {
                player.PlayerUnits.ForEach(x => x.UnitData.PlayerName = player.PlayerData.Name);
                player.Datacrons.ForEach(x => x.PlayerName = player.PlayerData.Name);
                player.Datacrons.ForEach(x => x.Name = m_datacronNames[x.Name]);
                player.Mods.ForEach(x => x.PlayerName = player.PlayerData.Name);
                player.Mods.ForEach(x => x.UnitName = unitDictionary[x.ToonId]);
                player.PlayerUnits.ForEach(x => x.UnitData.UnitStats.PhysicalDefense = Math.Round(x.UnitData.UnitStats.PhysicalDefense, 2));
                player.PlayerUnits.ForEach(x => x.UnitData.UnitStats.SpecialDefense = Math.Round(x.UnitData.UnitStats.SpecialDefense, 2));
                player.PlayerUnits.ForEach(x => x.UnitData.UnitStats.PhysicalCriticalChance = Math.Round(x.UnitData.UnitStats.PhysicalCriticalChance, 2));
                player.PlayerUnits.ForEach(x => x.UnitData.UnitStats.SpecialCriticalChance = Math.Round(x.UnitData.UnitStats.SpecialCriticalChance, 2));
                player.PlayerUnits.ForEach(x => x.UnitData.UnitStats.Potency = Math.Round(x.UnitData.UnitStats.Potency * 100, 2));
                player.PlayerUnits.ForEach(x => x.UnitData.UnitStats.Tenacity = Math.Round(x.UnitData.UnitStats.Tenacity * 100, 2));
                player.PlayerUnits.ForEach(x => x.UnitData.RelicTier = x.UnitData.RelicTier >= 3 ? x.UnitData.RelicTier - 2 : 0);
                player.Mods.ForEach(x => x.SecondaryStats.ForEach(y => y.Value = y.DisplayValue.Contains("%") ? y.Value / 100 : y.Value / 10000));
            });
        }

        private void BuildComparisonData()
        {
            DifferencesGuildData.OldGP = OldGuildData.Players.Sum(x => x.PlayerData.PlayerPower);
            DifferencesGuildData.NewGP = NewGuildData.Players.Sum(x => x.PlayerData.PlayerPower);

            foreach(var oldPlayerData in OldGuildData.Players)
            {
                var newPlayerData = NewGuildData.Players.SingleOrDefault(x => x.PlayerData.AllyCode == oldPlayerData.PlayerData.AllyCode);
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
            if (oldUnit == null)
            {
                foreach(var omicronName in newUnit.UnitData.AppliedOmicrons)
                    unitDifference.NewOmicrons.Add(newUnit.UnitData.UnitAbilities.Single(x => x.AbilityId == omicronName).AbilityName);
            }
            else if (oldUnit.UnitData.AppliedOmicrons.Count() < newUnit.UnitData.AppliedOmicrons.Count())
            {
                foreach (var omicronName in newUnit.UnitData.AppliedOmicrons.Where(x => !oldUnit.UnitData.AppliedOmicrons.Contains(x)))
                    unitDifference.NewOmicrons.Add(newUnit.UnitData.UnitAbilities.Single(x => x.AbilityId == omicronName).AbilityName);

                return true;
            }

            return false;
        }

        private bool IsZetaDifferences(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null)
            {
                foreach (var zetaName in newUnit.UnitData.AppliedZetas)
                    unitDifference.NewZetas.Add(newUnit.UnitData.UnitAbilities.Single(x => x.AbilityId == zetaName).AbilityName);
            }
            else if (oldUnit.UnitData.AppliedZetas.Count() < newUnit.UnitData.AppliedZetas.Count())
            {
                foreach (var zetaName in newUnit.UnitData.AppliedZetas.Where(x => !oldUnit.UnitData.AppliedZetas.Contains(x)))
                    unitDifference.NewZetas.Add(newUnit.UnitData.UnitAbilities.Single(x => x.AbilityId == zetaName).AbilityName);

                return true;
            }

            return false;
        }

        private bool IsGearLevelDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit?.UnitData.GearLevel < newUnit.UnitData.GearLevel)
            {
                unitDifference.OldGearLevel = oldUnit != null ? oldUnit.UnitData.GearLevel : 0;
                unitDifference.NewGearLevel = newUnit.UnitData.GearLevel;
                return true;
            }

            return false;
        }

        private bool IsRelicTierDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit?.UnitData.RelicTier < newUnit.UnitData.RelicTier)
            {
                unitDifference.OldRelicTier = oldUnit != null ? oldUnit.UnitData.RelicTier : 0;
                unitDifference.NewRelicTier = newUnit.UnitData.RelicTier;
                return true;
            }

            return false;
        }

        private bool IsRarityDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if (oldUnit == null || oldUnit?.UnitData.Rarity < newUnit.UnitData.Rarity)
            {
                unitDifference.OldRarity = oldUnit != null ? oldUnit.UnitData.Rarity: 0;
                unitDifference.NewRarity = newUnit.UnitData.Rarity;
                return true;
            }

            return false;
        }

        private bool IsUnitGPDifference(PlayerUnit oldUnit, PlayerUnit newUnit, UnitDifference unitDifference)
        {
            if(oldUnit == null || oldUnit?.UnitData.Power < newUnit.UnitData.Power)
            {
                unitDifference.OldGP = oldUnit != null ? oldUnit.UnitData.Power : 0;
                unitDifference.NewGP = newUnit.UnitData.Power;
                return true;
            }

            return false;
        }
    }
}
