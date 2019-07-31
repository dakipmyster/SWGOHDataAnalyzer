using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWGOHDBInterface;
using SWGOHMessage;
using System.IO;
using iText.Html2pdf;
using iText.Kernel.Pdf;

namespace SWGOHReportBuilder
{
    public class ReportBuilder
    {        
        DataBuilder m_dataBuilder;
        string m_playerGPDifferences;
        string m_UnitGPDifferences;
        string m_topTwentySection;
        string m_sevenStarSection;
        string m_gearTwelveToons;
        string m_gearThirteenToons;
        string m_zetasApplied;
        string m_journeyOrLegendaryUnlock;
        string m_journeyPrepared;
        string m_goldMembers;
        string m_detailedData;

        public ReportBuilder()
        {
            m_dataBuilder = new DataBuilder();
        }

        public bool CanRunReport()
        {
            return m_dataBuilder.CanRunReport();
        }

        public async Task CompileReport()
        {
            m_dataBuilder.GetSnapshotNames();
            
            List<Task> tasks = new List<Task>();

            string fileName = SWGOHMessageSystem.InputMessage("Enter in the filename for the report");

            SWGOHMessageSystem.OutputMessage("Processing Report....");

            tasks.Add(m_dataBuilder.CollectPlayerGPDifferences());
            tasks.Add(m_dataBuilder.CollectShipData());
            tasks.Add(m_dataBuilder.CollectUnitData());

            await Task.WhenAll(tasks.ToArray());

            await BuildReport(fileName);
        }

        private async Task BuildReport(string fileName)
        {
            StringBuilder pdfString = new StringBuilder();

            pdfString.Append(@"<html><head><style>
table, th, td {
  border: 1px solid black;
}
</style></head><body>");
            List<Task> tasks = new List<Task>();
                        
            tasks.Add(SevenStarSection());
            tasks.Add(GearTwelveToons());
            tasks.Add(GearThirteenToons());
            tasks.Add(ZetasApplied());
            tasks.Add(JourneyOrLegendaryUnlock());
            tasks.Add(JourneyPrepared());
            tasks.Add(GoldMembers());
            tasks.Add(FormatPlayerGPDifferences());
            tasks.Add(UnitGPDifferences());
            tasks.Add(TopTwentySection());
            tasks.Add(DetailedData());

            await Task.WhenAll(tasks.ToArray());

            pdfString.AppendLine(m_playerGPDifferences);
            pdfString.AppendLine(m_UnitGPDifferences);
            pdfString.AppendLine(m_topTwentySection);
            pdfString.AppendLine(m_sevenStarSection);
            pdfString.AppendLine(m_gearTwelveToons);
            pdfString.AppendLine(m_gearThirteenToons);
            pdfString.AppendLine(m_zetasApplied);
            pdfString.AppendLine(m_journeyOrLegendaryUnlock);
            pdfString.AppendLine(m_journeyPrepared);
            pdfString.AppendLine(m_goldMembers);
            pdfString.AppendLine(m_detailedData);

            pdfString.AppendLine(@"</body></html>");

            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SWGOHDataAnalyzer\\{fileName}.pdf";

            using (FileStream fs = File.Open(folderPath, FileMode.OpenOrCreate))
            {
                HtmlConverter.ConvertToPdf(pdfString.ToString(),fs, new ConverterProperties());
            }

            SWGOHMessageSystem.OutputMessage($"Report saved at {folderPath}");
        }

        private async Task UnitGPDifferences()
        {
            await Task.CompletedTask;            
        }

        private async Task GoldMembers()
        {
            await Task.CompletedTask;
        }

        private async Task JourneyPrepared()
        {
            await Task.CompletedTask;
        }

        private async Task JourneyOrLegendaryUnlock()
        {
            await Task.CompletedTask;
        }

        private async Task ZetasApplied()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been given zetas since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("<th>Zetas</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldZetas.Count < unit.NewZetas.Count)
                {
                    IEnumerable<string> zetaDifferences = unit.NewZetas.Except(unit.OldZetas);

                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine($"<td>{string.Join(",", zetaDifferences.ToArray())}</td>");
                    sb.AppendLine("</tr>");                    
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_zetasApplied = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task GearThirteenToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been geared to 13 since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");            
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if(unit.OldGearLevel < 13 && unit.NewGearLevel == 13)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine("</tr>");
                }
            }
            
            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");
                        
            m_gearThirteenToons = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task GearTwelveToons()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been geared to 12 since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldGearLevel < 12 && unit.NewGearLevel == 12)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_gearTwelveToons = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task TopTwentySection()
        {
            await Task.CompletedTask;
        }

        private async Task SevenStarSection()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section highlights all of the toons that have been 7*'ed since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");
            foreach (UnitData unit in m_dataBuilder.UnitData.OrderBy(a => a.PlayerName))
            {
                if (unit.OldRarity < 7 && unit.NewRarity == 7)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{unit.PlayerName}</td>");
                    sb.AppendLine($"<td>{unit.UnitName}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            sb.AppendLine("This section highlights all of the ships that have been 7*'ed since the last snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Toon</th>");
            sb.AppendLine("</tr>");
            foreach (ShipData ship in m_dataBuilder.ShipData.OrderBy(a => a.PlayerName))
            {
                if (ship.OldRarity < 7 && ship.NewRarity == 7)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{ship.PlayerName}</td>");
                    sb.AppendLine($"<td>{ship.ShipName}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_sevenStarSection = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task FormatPlayerGPDifferences()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("This section goes over the Galatic Power (GP) differences for players between snapshots.  Here is the top ten players who have gained the most Galatic Power by total and by percentage from the previous snapshot.");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Previous Galatic Power</th>");
            sb.AppendLine("<th>New Galatic Power</th>");
            sb.AppendLine("<th>Galatic Power Increase</th>");
            sb.AppendLine("</tr>");
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerDifference).Take(10))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{player.PlayerName}</td>");
                sb.AppendLine($"<td>{player.OldGalaticPower}</td>");
                sb.AppendLine($"<td>{player.NewGalaticPower}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerDifference}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Previous Galatic Power</th>");
            sb.AppendLine("<th>New Galatic Power</th>");
            sb.AppendLine("<th>Galatic Power % Increase</th>");
            sb.AppendLine("</tr>");
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderByDescending(a => a.GalaticPowerPercentageDifference).Take(10))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{player.PlayerName}</td>");
                sb.AppendLine($"<td>{player.OldGalaticPower}</td>");
                sb.AppendLine($"<td>{player.NewGalaticPower}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerPercentageDifference}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p/>");

            m_playerGPDifferences = sb.ToString();

            await Task.CompletedTask;
        }

        private async Task DetailedData()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("For those who are interested, here is some full table data that the stats refer to.");
            sb.AppendLine("Here is the full list of players and their Galatic Power differences.");

            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Player Name</th>");
            sb.AppendLine("<th>Previous Galatic Power</th>");
            sb.AppendLine("<th>New Galatic Power</th>");
            sb.AppendLine("<th>Galatic Power Increase</th>");
            sb.AppendLine("<th>Galatic Power % Increase</th>");
            sb.AppendLine("</tr>");
            foreach (PlayerData player in m_dataBuilder.PlayerData.OrderBy(a => a.PlayerName).ToList())
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{player.PlayerName}</td>");
                sb.AppendLine($"<td>{player.OldGalaticPower}</td>");
                sb.AppendLine($"<td>{player.NewGalaticPower}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerDifference}</td>");
                sb.AppendLine($"<td>{player.GalaticPowerPercentageDifference}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            m_detailedData = sb.ToString();

            await Task.CompletedTask;
        }
    }
}

/*
Top 10 of every stat
Who 7* toon
Who G13 toon
Top 10 GP jump by number
Top 10 GP jump by percentage of their previous/current GP (edited) 
Kyle L 2:59 PM
Gold Group Members
Zetas Applied
Kyle L 3:34 PM
Journey or Legendary unlock
*/
