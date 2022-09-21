using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SWGOHInterface;
using SWGOHMessage;
using System.Data;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace SWGOHDBInterface
{
    public class DBInterface
    {
        #region Private Members

        private string m_folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer";
        private string m_snapshotName;
        #endregion

        #region Public Members

        public bool HasOldSnapshots => Tables.Count() > 1;

        public List<string> Tables { get; } = new List<string>();

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbName">Name of the snapshot the user wants</param>
        public DBInterface(string dbTableName)
        {
            if (!Directory.Exists(m_folderPath))
                Directory.CreateDirectory(m_folderPath);

            m_snapshotName = dbTableName;

            CollectSnapshots();
        }

        public DBInterface()
        {
            CollectSnapshots();
        }

        /// <summary>
        /// Method to collect the name of all the snapshots
        /// </summary>
        private void CollectSnapshots()
        {
            Tables.AddRange(Directory.GetFiles(m_folderPath, "*.json", SearchOption.TopDirectoryOnly));
        }

        public void WriteDataToJsonFile(List<Player> players)
        {
            Parallel.ForEach(players.AsEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (player) =>
            {
                SWGOHMessageSystem.OutputMessage($"Sanitizing mods for player {player.PlayerData.Name}");

                foreach (var mod in player.Mods)
                    SanitizeModData(mod);
            });

            SWGOHMessageSystem.OutputMessage($"Saving snapshot");

            using (var file = File.CreateText($"{m_folderPath}\\{m_snapshotName}.json"))
            {
                var jsonWriter = new JsonTextWriter(file);

                JsonSerializer.CreateDefault().Serialize(jsonWriter, players);
            }

            using (StreamReader file = File.OpenText($"{m_folderPath}\\{m_snapshotName}.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                List<Player> movie2 = (List<Player>)serializer.Deserialize(file, typeof(List<Player>));
            }
        }

        /// <summary>
        /// Cleans up data before its stored
        /// </summary>
        /// <param name="modData">Mod data for character</param>
        private void SanitizeModData(Mod modData)        
        {
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

            switch(modData.Slot)
            {
                case "1":
                    modData.Slot = "Square";
                    break;

                case "2":
                    modData.Slot = "Arrow";
                    break;

                case "3":
                    modData.Slot = "Diamond";
                    break;

                case "4":
                    modData.Slot = "Triangle";
                    break;

                case "5":
                    modData.Slot = "Circle";
                    break;

                case "6":
                    modData.Slot = "Cross";
                    break;
            }

            for(int secondaryStatPosition = 0; secondaryStatPosition >= 4; secondaryStatPosition++)
            {
                var secondaryDetails = modData.SecondaryStats.ElementAtOrDefault(secondaryStatPosition);

                if (secondaryDetails == null)
                    continue;

                if (!String.IsNullOrEmpty(secondaryDetails.Value) 
                    && !String.IsNullOrEmpty(secondaryDetails.Name) 
                    && secondaryDetails.Value.Contains("%") 
                    && !secondaryDetails.Name.Contains("Potency") 
                    && !secondaryDetails.Name.Contains("Critical Chance") 
                    && !secondaryDetails.Name.Contains("Tenacity"))
                {
                    secondaryDetails.Name = $"{secondaryDetails.Name} %";
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

            using (SQLiteConnection m_dbConnection = new SQLiteConnection($"Data Source={m_folderPath}\\{m_snapshotName}"))
            {
                m_dbConnection.Open();

                using (SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection))
                {
                    command.CommandType = CommandType.Text;

                    if (parameters != null)
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
